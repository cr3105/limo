# Generate the Excel output files for existing tracks in the database

from mysql.connector        import connect                                      # connect to MySQL db
from mysql.connector.errors import Error                    as MySQL_Error      # MySQL error container
from mysql.connector        import errorcode                as MySQL_errorcode  # MySQL error codes
from os                     import environ                                      # access to environment variables
from json                   import loads                                        # str -> dict conversion
from timeit                 import default_timer            as timer
import os
import xlsxwriter

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

def main() -> None:
    """
    Program entry point.

    :return: Nothing
    """

    try:
        connection = connect_to_db2()
        kwargs = {'year_to_schedule': 2018}
        start = timer()
        result = run(connection, **kwargs)
        output_results(result, connection)
        end = timer()
        print(f'time elapsed: {end - start}')
        connection.close()


    except Exception as e:
        print(f'Something broke ...\n\tReason:{str(e)}')
        connection.close()
        exit(1)

    return None

def output_results(result: tuple, connection: object) -> None:

    student_info = get_student_info(connection)

    #check for output folder and create if necessary
    if not os.path.exists(f'./RESULTS/') :
        os.mkdir(f'./RESULTS/')

    for i, _ in enumerate(result[0]):
        if len(result[0][i]) == 0:
            continue

        print("*" * 75)
        print("\nBegin Track {}: ".format(i + 1))

        print("*" * 75)

        #XLSX file output
        workbook = xlsxwriter.Workbook('./RESULTS/track_{}.xlsx'.format(i+1))
        worksheet = workbook.add_worksheet()

        worksheet.write(0, 1,'Schueler-Nr.')
        worksheet.write(0, 2,'SchuelerNachname')
        worksheet.write(0, 3,'SchuelerRufname')
        worksheet.write(0, 4,'Klasse')
        worksheet.write(0, 5,'Kurs')

        row = 1
        for course in result[0][i]:
            course[0].sort()
            for student_id in course[0]:
                worksheet.write(row, 1, student_id)
                worksheet.write(row, 5, course[1])
                if str(student_id) in student_info:
                    worksheet.write(row, 2, student_info[str(student_id)][1])
                    worksheet.write(row, 3, student_info[str(student_id)][0])
                    worksheet.write(row, 4, '{}{}'.format(student_info[str(student_id)][2],student_info[str(student_id)][3]))
                else:
                    worksheet.write(row, 2, 'Information')
                    worksheet.write(row, 3, 'Not')
                    worksheet.write(row, 4, 'Available')
                row += 1
            
            worksheet.write(row, 0, 'Anzahl Schueler: {}'.format(len(course[0])))
            row += 2

        worksheet.write(row, 0, 'Anzahl Kurse: {}'.format(len(result[0][i])))
        row += 2

        worksheet.write(row, 0, 'Not assigned:')
        row += 1
        for course in result[1][i]:
            if len(course) == 0:
                continue
            course[0].sort()
            for student_id in course[0]:
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
            worksheet.write(row, 0, 'Anzahl Schueler: {}'.format(len(course[0])))
            workbook.close()

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

def run(connection: object, **kwargs: dict) -> tuple:

    if connection is None:
        print('Invalid connection received! Terminating ...')
        exit(1)

    track_date = kwargs['year_to_schedule']    

    # get course dictionary
    course_dict = dict()
    classes_command = "SELECT * FROM available_courses ORDER BY type;"
    available_courses, rows = execute_db_command(connection, classes_command, 0)

    for acourse in available_courses:
        course_name = '{}{}'.format(acourse[1], acourse[2])
        course_dict[course_name] = acourse[0]

    leftovers = [[] for i in range(10)]
    result = [[] for i in range(10)]

    # read all tracks from database
    current_index = 0
    for track_id in range(1, 10):
        query1 = "SELECT student_id FROM course_assignments \
            WHERE track_date = '{}' AND track = '{}' AND course_id = '{}';".format(track_date,track_id, -1)
        un_track, rows1 = execute_db_command(connection, query1, 0)
        if rows1 > 0:
            l1 = [id[0] for id in un_track]
            leftovers[current_index].append((l1,'UN'))
        for course in course_dict:
            course_id = course_dict[course]
            query = "SELECT student_id FROM course_assignments \
                    WHERE track_date = '{}' AND track = '{}' AND course_id = '{}';".format(track_date,track_id, course_id)
            temp_track, rows = execute_db_command(connection, query, 0)
            if rows > 0:
                l = [id[0] for id in temp_track]
                result[current_index].append((l,course))
                
        current_index +=1


    return (result, leftovers)


if __name__ == '__main__':
    main()


