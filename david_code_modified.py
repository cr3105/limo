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

module_commands = ["SELECT StudentID FROM modulwahl WHERE D1 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D2 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D3 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D4 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D5 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D6 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D7 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D8 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D9 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE D10 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E1 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E2 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E3 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E4 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E5 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E6 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E7 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E8 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E9 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE E10 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M1 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M2 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M3 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M4 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M5 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M6 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M7 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M8 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M9 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE M10 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE F1 = 1 OR F2 = 1 OR F3 = 1 OR F4 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S1 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S2 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S3 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S4 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S5 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S6 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S7 = 1;",
                   "SELECT StudentID FROM modulwahl WHERE S8 = 1;"]

def call_storedproc_in_db(connection: object, procname: str, *args: tuple) -> list:
    results = None
    cursor = None

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
            cursor.callproc(procname)
            print(cursor.statement)
            

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
        results = [item for item in cursor.stored_results()]

        # close the cursor to avoid memory leaks
        cursor.close()

    return results


def execute_db_command(connection: object, command: str, *args: tuple) -> list:
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
            #connection.commit()

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
        results = [item for item in cursor]

        # close the cursor to avoid memory leaks
        cursor.close()

    return results


def modulliste(connection: object, modul_id: int) -> list:
    """
    Builds the list of all students who selected ``modul_id``.

    :param modul_id: The id of the modul to retrieve.
    :return: A list of student ids representing the pool of student's who've chosen this modul.
    """
    global module_commands;

    return [id[0] for id in execute_db_command(connection, module_commands[modul_id])]


def schuelerliste(connection: object) -> list:
    """
    Schuelerliste fuer die Schiene anlegen

    :return: A list containing the ids of all students.
    """
    query = "SELECT StudentID FROM modulwahl;"
    return [id[0] for id in execute_db_command(connection, query)]


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


def connect_to_db() -> object:
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


def get_modules(connection: object, **kwargs: dict) -> dict:
    laengen = [ kwargs['deutsch'], kwargs['english'], kwargs['mathe'], kwargs['franz'], kwargs['segels'] ]
    prefixes = ['D', 'E', 'M', 'F', 'S']

    modullisten = {'D': [], 'E': [], 'M': [], 'F': [], 'S': []}
    # modullisten (student_ids, modul_name)

    cmd_selector = 0
    for length, prefix in zip(laengen, prefixes):  
        for cur in range(length):  					
            modullisten[prefix].append((modulliste(connection, cmd_selector), '{}{}'.format(prefix, cur + 1)))
            cmd_selector = cmd_selector + 1


    return modullisten


def main() -> None:
    """
    Program entry point.

    :return: Nothing
    """

    # Modul fuellen
    seed()

    try:
        connection = connect_to_db()
        kwargs = {'deutsch': 10, 'english': 10, 'mathe': 10, 'franz': 1, 'segels': 8, 'num_sections': 9, 'num_courses': 10}
        start = timer()
        result = run(connection, **kwargs)
        end = timer()
        output_results(result, connection)
        print(f'time elapsed: {end - start}')
        connection.close()


    except Exception as e:
        print(f'Something broke ...\n\tReason:{str(e)}')
        connection.close()
        exit(1)

    return None


def output_results(result: tuple, connection) -> None:
    unique_classes = {}

    student_info = get_student_info(connection)

    for i, temp in enumerate(result[0]):
        print("*" * 75)
        print("\nBegin Section {}: ".format(i + 1))
        seen = {}

        for modul in temp:
            print(f"{modul[1]}: {modul[0]}\n\tAnzahl Schueler: {len(modul[0])}\n")
            unique_classes[modul[1]] = modul[1]

            for s in modul[0]:
                if s in seen:
                    print(f'Student {s} is in multiple modules!')
                else:
                    seen[s] = s
        print(f'Unassigned students for this section ({len(result[1][i])}): {result[1][i]}')

        print("*" * 75)

    test = list(unique_classes)
    test.sort()
    print(f'Unique courses assigned: {test}')
    format_string = ',{},{},{},{},{},\n'
    if not os.path.exists(f'./RESULTS/') :
        os.mkdir(f'./RESULTS/')

    for i, _ in enumerate(result[0]):
        with open(f'./RESULTS/section{i+1}.txt', 'w') as f:
            for modul in result[0][i]:
                f.write(f"{modul[1]}: {modul[0]}\n\tAnzahl Schueler: {len(modul[0])}\n")
            f.write(f'Unassigned students for this section ({len(result[1][i])}): {result[1][i]}')

        for i, _ in enumerate(result[0]):
            with open(f'./RESULTS/zuordnung_schiene_{i+1}.csv', 'w') as f:
                f.write(format_string.format('Schueler-Nr.','SchuelerNachname','SchuelerRufname','Klasse','Kurs'))
                for modul in result[0][i]:
                    for student_id in modul[0]:
                        if str(student_id) in student_info: 
                            f.write(format_string.format(student_id, student_info[str(student_id)][0], 
                                                         student_info[str(student_id)][1], student_info[str(student_id)][2], modul[1]))
                        else:
                            f.write(format_string.format(student_id, 'Information', 'Not', 'Available', modul[1]))
                    f.write('Anzahl Schueler: {},\n'.format(len(modul[0])))
                    f.write(format_string.format('', '', '', '', ''))
                
                for student_id in result[1][i]:
                    f.write(format_string.format(student_id, student_info[str(student_id)][0], 
                                                     student_info[str(student_id)][1], student_info[str(student_id)][2], 'unassigned'))
                f.write('Anzahl Schueler: {},\n'.format(len(result[1][i])))

    return None


def get_student_info(connection):

    query = f'SELECT StudentID, fname, lname, sub_class FROM modulwahl;'

    result = execute_db_command(connection, query)

    if result:
        ret = {}
        for student in result:
            ret[str(student[0])] = (student[1], student[2], student[3])

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


def pick_modul(modulliste, students, used_courses) -> int: 
    found_index = -1
    max_found = -1

    modulliste.sort()
    
    for i, l in enumerate(modulliste):
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


def first_section(modullisten) -> list:
    firstS = []
    firstS.append(([7,12,14,19,22,31,33,41,45,48,56,79,83,85,90,95,100,110,112,134],'D1'))
    firstS.append(([17,20,21,27,29,34,71,72,73,75,92,113,124,130,137,149,152,159,174,354,355],'D2'))
    firstS.append(([62,88,93,120,142,190,199,213,263,296,314,318,321,323,324,330,336,346,350],'D5'))
    firstS.append(([24,37,39,59,61,69,76,80,98,114,129,135,147,312,327,342],'E4'))
    firstS.append(([3,9,15,16,18,40,42,47,52,55,64,78,86,107,109,115,117,122,125,128],'E6'))
    firstS.append(([139,144,146,153,162,164,177,184,193,289,294,316,317,322,347,348,349,351,352],'E7'))
    firstS.append(([132,143,154,158,160,165,173,176,178,179,192,287,288,290,293,298,301,304,331],'M1'))
    firstS.append(([6,8,10,13,35,36,38,43,46,84,87,97,104,121,123,150,151,161,291],'M3'))
    firstS.append(([1,2,4,5,23,25,26,49,51,53,66,67,68,70,74,77,89,163,182],'M7'))
    firstS.append(([11,28,30,32,44,50,54,57,63,65,81,82,133,167,188,297,332,338,343,356,353],'S7'))

    student_list = [i for i in range(368)]

    for l in firstS:
        remove_set_difference(student_list, l[0])
        remove_set_difference(modullisten[l[1][0]][int(l[1][1]) - 1][0], l[0])

    return firstS


def run(connection: object, **kwargs: dict) -> None:
    """
    Program main thread for building the section lists and sending them to the database for permanent storage

    :param connection: A valid MySQL connection object.
    :type connection: :class:``MySQLConnection``
    :return: Nothing
    """

    if connection is None:
        print('Invalid connection received! Terminating ...')
        exit(1)

    leftovers = [[] for i in range(kwargs['num_sections'])]
    result = [[] for i in range(kwargs['num_sections'])]
    student_set = schuelerliste(connection)
    modullisten = get_modules(connection, **kwargs)
    modul_ids = ['D','E','M','S']
    id_perms = list(permutations(modul_ids, len(modul_ids)))
    # 1 E, D, M pro section
    # all french in one course
    french_needed = 2
    
    result[0] = (first_section(modullisten))

    for current_section, leftover in zip(result, leftovers):

        if len(current_section) > 0:
            continue

        courses_needed = kwargs['num_courses']
        used_courses = {}
        temp_students = set(student_set)
        
        if french_needed:
            current_section.append((modullisten['F'][0][0], modullisten['F'][0][1]))
            remove_set_difference(temp_students, modullisten['F'][0][0])
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

            temp_section = generate_section(temp_modullisten, list(temp_students), ordering, courses_needed)
            if len(temp_section[1]) < best:
                best_section = temp_section
                best = len(temp_section[1])
        
        current_section.extend(best_section[0])
        leftover.extend(list(best_section[1]))
        modullisten = best_section[2]

    return (result, leftovers)


def generate_section(modullisten, temp_students, modul_ids, count_courses):
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


