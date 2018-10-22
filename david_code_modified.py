# Listen fuer Module anfertigen

from mysql.connector        import connect                                      # connect to MySQL db
from mysql.connector.errors import Error                    as MySQL_Error      # MySQL error container
from mysql.connector        import errorcode                as MySQL_errorcode  # MySQL error codes
from random                 import sample, seed, randint, shuffle               # random student selection
from os                     import environ                                      # access to environment variables
from json                   import loads                                        # str -> dict conversion
from timeit                 import default_timer            as timer
from concurrent.futures     import ThreadPoolExecutor
from itertools              import permutations
import os
import xlsxwriter

course_dict = dict()
confirmed_tracks = None

def execute_db_command(connection: object, command: str, commit: int, *args: tuple) -> (list, int):
    """
    Executes the given MySQL ``command`` on the MySQL server in ``connection``. Arguments may be passed via the tuple

    :param connection: A valid MySQL connection object.
    :param command: A valid and ready-to-execute MySQL query.
    :param args: Optional tuple of parameters for the MySQL query.
    :type connection: :class:``MySQLConnection``
    :type command: str
    :type args: tuple
    :return: A list containing the results of the given query, or an empty list if no results were returned by the
             database. If a fatal error occurs, ``None`` is returned.
    """

    results = None
    cursor = None
    rows = 0

    # ensure we have received a valid connection object
    if not connection or connection is None:
        print(f'Received invalid MySQLConnection object!\n\tConnection is {type(connection)} expected MySQLConnection.')

    else:
        try:
            # create a cursor object for this transaction
            cursor = connection.cursor()

        except MySQL_Error as e:
            cursor.close()

            if hasattr(e, 'errno'):
                print(f'Executing the query failed due to MySQL error {e.errno}.\n\tReason: {str(e)}')
            else:
                print(f'Executing the query failed due to MySQL error.\n\tReason: {str(e)}')

            return None

        except Exception as e:
            cursor.close()

            print(f'Creating cursor object failed due to unknown error.\n\tReason: {str(e)}')
            return None

        try:
            # execute the command
            cursor.execute(command, params=args)
            # commit the mysql transaction
            if commit == 1:
                connection.commit()

        except MySQL_Error as e:
            # if an error occurs we need to let MySQL know that the last transaction(s) should be discarded
            print('An error occurred, rolling back changes ...')
            connection.rollback()

            if hasattr(e, 'errno'):
                print(f'Executing the query failed due to MySQL error {e.errno}.\n\tReason: {str(e)}')
            else:
                print(f'Executing the query failed due to MySQL error.\n\tReason: {str(e)}')

            return None

        except Exception as e:
            # if an error occurs we need to let MySQL know that the last transaction(s) should be discarded
            print('An error occurred, rolling back changes ...')
            connection.rollback()

            print(f'Executing the query failed due to unknown error.\n\tReason: {str(e)}')
            return None

        # create a list of results
        if commit == 0:
            results = cursor.fetchall()
            rows = cursor.rowcount


        # close the cursor to avoid memory leaks
        cursor.close()

    return (results, rows)

def modulliste(connection: object, modul_id: int) -> list:
    """
    Builds the list of all students who selected ``modul_id``.

    :param modul_id: The id of the modul to retrieve.
    :return: A list of student ids representing the pool of student's who've chosen this modul.
    """
    module_commands = "SELECT student_id FROM student_choices WHERE course_id = {0};"
    module_list, rows = execute_db_command(connection, module_commands.format(modul_id), 0)
    return [id[0] for id in module_list]

def schuelerliste(connection: object) -> list:
    """
    Schuelerliste fuer die Schiene anlegen

    :return: A list containing the ids of all students.
    """
    query = "SELECT id FROM students;"
    student_list, rows = execute_db_command(connection, query, 0)
    return [id[0] for id in student_list]

def zuteilen(source: set, students: set) -> list:
    """
    Schueler auswaehlen und zuteilen

    :param source: eine Modulliste
    :param students: freie SuS
    :return: list of students assigned to source
    """

    # SuS waehlen
    picked = []
    CLASS_SIZE = 20

    while len(picked) < CLASS_SIZE:
        current = sample(source, 50 if len(source) >= 50 else len(source))
        counter = 0
        for s in source:  # gibt es noch einen freien SuS?
            if s in students:
                break  # wenn freier SuS, Abbruch
            else:
                counter += 1

        if counter == len(source):
            break  # beendet aeussere while-loop

        for s in current:  # Topf wird gefuellt
            if len(picked) >= CLASS_SIZE:
                break
            if s not in picked and s in students:
                picked.append(s)  # wenn SuS in current & kein Modul dann picked
                if type(source) is list:
                    source.remove(s)
                elif type(source) is set:
                    source.discard(s)
                if type(students) is list:
                    students.remove(s)
                elif type(students) is set:
                    students.discard(s)
                
        if len(source) == 0 or len(students) == 0:
            break

            # Rueckgabe der Kurslisten
    return picked

def connect_to_db2() -> object:
    """
    Attempts to establish database connection.

    :return: A MySQLConnection object if connection to the DB succeeds, otherwise None.
    """
    connection = None

    try:
        if 'limoinput_connection' in environ:
            config = loads(environ['limoinput_connection'])
            connection = connect(**config)
            print("Verbindung erfolgreich\n")

        else:
            print('SQL connection string not found!')

    except Exception as e:
        print(f"Verbindung fehlgeschlagen!\n\tReason:{str(e)}")

    return connection

def get_modules(connection: object) -> (dict,list):

    #load the list of available courses from the database
    global course_dict
    classes_command = "SELECT * FROM available_courses ORDER BY type;"

    available_courses, rows = execute_db_command(connection, classes_command, 0)

    prefixes = []
    laengen = []
    modullisten = dict([])
    modul_ids = []

    #go through the course list and create the list with prefixes 
    #and the number of courses of the same type
    #create the empty dictionary 'modullisten' based on the course list
    #create a dictionary 'course_dict' to translate between course name and ID
    if available_courses:
        last_prefix = available_courses[0][1]
        last_length = 0
        for acourse in available_courses:
            course_name = '{}{}'.format(acourse[1], acourse[2])
            course_dict[course_name] = acourse[0]
            if last_prefix == acourse[1]:
                last_length += 1
            else:
                prefixes.append(last_prefix)
                laengen.append(last_length)
                modullisten[last_prefix] = []
                last_prefix = acourse[1]
                last_length = 1
        prefixes.append(last_prefix)
        laengen.append(last_length)
        modullisten[last_prefix] = []
    else:
        exit(1)

    #modullisten = {'D': [], 'E': [], 'F': [], 'M': [], 'S': []}

    for length, prefix in zip(laengen, prefixes):  
        if length > 1:
            modul_ids.append(prefix)

            for cur in range(length):  					
                modullisten[prefix].append((modulliste(connection, course_dict['{}{}'.format(prefix, cur + 1)]), '{}{}'.format(prefix, cur + 1)))
        else:
            del modullisten[prefix]

    return (modullisten, modul_ids)

def main() -> None:
    """
    Program entry point.

    :return: Nothing
    """

    # Modul fuellen
    seed()

    try:
        connection = connect_to_db2()
        kwargs = {'year_to_schedule': 2018, 'num_courses': 10}
        start = timer()
        result = run(connection, **kwargs)
        end = timer()
        output_results(result, connection, **kwargs)
        print(f'time elapsed: {end - start}')
        connection.close()


    except Exception as e:
        print(f'Something broke ...\n\tReason:{str(e)}')
        connection.close()
        exit(1)

    return None

def output_results(result: tuple, connection: object, **kwargs: dict) -> None:
    unique_classes = {}
    format_string  = ',{},{},{},{},{},\n'
    format_string2 = ',{},{},{},{}{},{},\n'

    global confirmed_tracks

    student_info = get_student_info(connection)
    year = kwargs['year_to_schedule']    

    #check for output folder and create if necessary
    if not os.path.exists(f'./RESULTS/') :
        os.mkdir(f'./RESULTS/')

    for i, _ in enumerate(result[0]):
        print("*" * 75)
        print("\nBegin Track {}: ".format(i + 1))
        seen = {}

        #command line output
        for track in result[0][i]:
            track[0].sort()
            print(f"{track[1]}: {track[0]}\n\tAnzahl Schueler: {len(track[0])}\n")
            unique_classes[track[1]] = track[1]

            for s in track[0]:
                if s in seen:
                    print(f'Student {s} is in multiple modules!')
                else:
                    seen[s] = s
        print(f'Unassigned students for this track ({len(result[1][i])}): {result[1][i]}')

        print("*" * 75)

        #text file output
        with open(f'./RESULTS/track{i+1}.txt', 'w') as f:
            for track in result[0][i]:
                f.write(f"{track[1]}: {track[0]}\n\tAnzahl Schueler: {len(track[0])}\n")
            f.write(f'Unassigned students for this track ({len(result[1][i])}): {result[1][i]}')
            f.close()

        #CSV file output
        workbook = xlsxwriter.Workbook('./RESULTS/track_{}.xlsx'.format(i+1))
        worksheet = workbook.add_worksheet()

        worksheet.write(0, 1,'Schueler-Nr.')
        worksheet.write(0, 2,'SchuelerNachname')
        worksheet.write(0, 3,'SchuelerRufname')
        worksheet.write(0, 4,'Klasse')
        worksheet.write(0, 5,'Kurs')

        row = 1
        for track in result[0][i]:
            for student_id in track[0]:
                worksheet.write(row, 1, student_id)
                worksheet.write(row, 5, track[1])
                if str(student_id) in student_info:
                    worksheet.write(row, 2, student_info[str(student_id)][1])
                    worksheet.write(row, 3, student_info[str(student_id)][0])
                    worksheet.write(row, 4, '{}{}'.format(student_info[str(student_id)][2],student_info[str(student_id)][3]))
                else:
                    worksheet.write(row, 2, 'Information')
                    worksheet.write(row, 3, 'Not')
                    worksheet.write(row, 4, 'Available')
                row += 1
            
            worksheet.write(row, 0, 'Anzahl Schueler: {}'.format(len(track[0])))
            row += 2

        worksheet.write(row, 0, 'Anzahl Kurse: {}'.format(len(result[0][i])))
        row += 2

        worksheet.write(row, 0, 'Not assigned:')
        row += 1
        for student_id in result[1][i]:
            worksheet.write(row, 1, student_id)
            worksheet.write(row, 5, 'unassigned')
            if str(student_id) in student_info:
                worksheet.write(row, 2, student_info[str(student_id)][1])
                worksheet.write(row, 3, student_info[str(student_id)][0])
                worksheet.write(row, 4, '{}{}'.format(student_info[str(student_id)][2],student_info[str(student_id)][3]))
            else:
                worksheet.write(row, 2, 'Information')
                worksheet.write(row, 3, 'Not')
                worksheet.write(row, 4, 'Available')
            row += 1
        worksheet.write(row, 0, 'Anzahl Schueler: {}'.format(len(result[1][i])))
        workbook.close()

        # database output_results (only newly generated courses, skip confirmed ones
        if (i+1) in confirmed_tracks:
            continue

        update_cmd = "INSERT INTO course_assignments (track_date, track, course_id, student_id, isLocked) \
                      VALUES ('{0}', '{1}', '{2}', '{3}', '{4}');"

        for track in result[0][i]:
            course_id = course_dict[track[1]] 
            for student_id in track[0]:
                execute_db_command(connection, update_cmd.format(year,i+1,course_id,student_id,0), 1)

        for student_id in result[1][i]:
            execute_db_command(connection, update_cmd.format(year,i+1,-1,student_id,0), 1)

    test = list(unique_classes)
    test.sort()
    print(f'Unique courses assigned: {test}')

    return None

def get_student_info(connection):

    query = f'SELECT students.id, students.fname, students.lname, classes.grade, classes.class FROM students \
              INNER JOIN classes ON students.class = classes.id;'

    result, rows = execute_db_command(connection, query, 0)

    if result:
        ret = {}
        for student in result:
            ret[str(student[0])] = (student[1], student[2], student[3], student[4])

        return ret
    else:
        raise ValueError('No student info found!')

def remove_set_difference(list1, list2) -> None:
    for i in list2:
        if i in list1:
            if type(list1) is list:
                list1.remove(i)
            elif type(list1) is set:
                list1.discard(i)

    return None

def pick_modul(modul_liste, students, used_courses) -> int: 
    found_index = -1
    max_found = -1

    modul_liste.sort()
    
    for i, l in enumerate(modul_liste):
        counter = 0

        if l[1] in used_courses:
            continue

        for s in students:
            if s in l[0]:
                counter += 1

        if counter > max_found:
            max_found = counter
            found_index = i
    
    return found_index

def get_confirmed_track(connection, modullisten, track_date, track_id) -> (list, int):
    firstS = []
    hasFrench = 0;
    
    for course in course_dict:
        course_id = course_dict[course]
        query = "SELECT student_id FROM course_assignments \
            WHERE track_date = '{}' AND track = '{}' AND course_id = '{}';".format(track_date,track_id, course_id)
        confirmed_section, rows = execute_db_command(connection, query, 0)
        if rows > 0:
            l = [id[0] for id in confirmed_section]
            firstS.append((l,course))


    student_list = [i for i in range(400)]

    for l in firstS:
        remove_set_difference(student_list, l[0])
        if l[1] == 'F1':
            hasFrench = 1
        else:
            remove_set_difference(modullisten[l[1][0]][int(l[1][1]) - 1][0], l[0])

    return (firstS, hasFrench)

def run(connection: object, **kwargs: dict) -> tuple:
    """
    Program main thread for building the track lists and sending them to the database for permanent storage

    :param connection: A valid MySQL connection object.
    :type connection: :class:``MySQLConnection``
    :return: Nothing
    """
    global confirmed_tracks

    if connection is None:
        print('Invalid connection received! Terminating ...')
        exit(1)

    # determine number of lists needed to store sections:
    # we need the number of already confirmed sections in the database plus one
    year = kwargs['year_to_schedule']    
    query = 'SELECT confirmed_modules.track FROM confirmed_modules WHERE track_date="{}" ORDER BY track;'.format(year)
    temp_list, num_sections = execute_db_command(connection, query, 0)
    num_sections += 1 
    confirmed_tracks = [item[0] for item in temp_list]

    leftovers = [[] for i in range(num_sections)]
    result = [[] for i in range(num_sections)]
    student_set = schuelerliste(connection)
    modullisten, modul_ids = get_modules(connection)
    id_perms = list(permutations(modul_ids, len(modul_ids)))
    # 1 E, D, M pro track
    # all french in one course, repeat once
    french_needed = 2

    # read all assigned and confirmed sections back from database
    current_id = 0
    for track_id in confirmed_tracks:
        result[current_id], hasFrench = (get_confirmed_track(connection, modullisten, year, track_id))
        if hasFrench == 1:
            french_needed -= 1
        current_id += 1

    # delete all unconfirmed sections from database
    query = "DELETE FROM course_assignments WHERE (isLocked = '0');"
    execute_db_command(connection, query, 1)

    for current_section, leftover in zip(result, leftovers):
        if len(current_section) > 0:
            continue

        courses_needed = kwargs['num_courses']
        used_courses = {}
        temp_students = set(student_set)
        
        if french_needed:
            french_class = modulliste(connection, course_dict['F1'])
            current_section.append((french_class, 'F1'))
            remove_set_difference(temp_students, french_class)
            french_needed -= 1
            courses_needed -= 1
        

        best_section = None
        best = float('inf')
        for ordering in id_perms:
            temp_modullisten = {}
            for k,v in modullisten.items():
                temp_modullisten[k] = []
                for l in v:
                    temp_modullisten[k].append((list(l[0]), str(l[1])))

            temp_section = generate_track(temp_modullisten, list(temp_students), ordering, courses_needed)
            if len(temp_section[1]) < best:
                best_section = temp_section
                best = len(temp_section[1])
        
        current_section.extend(best_section[0])
        leftover.extend(list(best_section[1]))
        modullisten = best_section[2]

    return (result, leftovers)

def generate_track(modullisten, temp_students, modul_ids, count_courses):
    used_courses = {}
    current_section = []

    for i, _ in enumerate(range(count_courses)):
        picked_index = pick_modul(modullisten[modul_ids[i % 4]], temp_students, used_courses)
        current_section.append((zuteilen(modullisten[modul_ids[i % 4]][picked_index][0], temp_students),
                                modullisten[modul_ids[i % 4]][picked_index][1]))
        used_courses[modullisten[modul_ids[i % 4]][picked_index][1]] = modullisten[modul_ids[i % 4]][picked_index][1]

    return (current_section, temp_students, modullisten)


if __name__ == '__main__':
    main()


