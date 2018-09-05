# Listen fuer Module anfertigen

from mysql.connector        import connect                                      # connect to MySQL db
from mysql.connector.errors import Error                    as MySQL_Error      # MySQL error container
from mysql.connector        import errorcode                as MySQL_errorcode  # MySQL error codes
from random                 import sample, seed, randint                        # random student selection
from os                     import environ                                      # access to environment variables
from json                   import loads                                        # str -> dict conversion


def modulliste(modul_id: str) -> set:
    """
    Builds the list of all students who selected ``modul_id``.

    :param modul_id: The id of the modul to retrieve.
    :return: A list of student ids representing the pool of student's who've chosen this modul.
    """

    global cursor

    query = f"SELECT * FROM modulwahl WHERE %s = 1"
    cursor.execute(query, modul_id)
    module = set()

    for modul in cursor:
        module.add(modul[0])

    return module


def schuelerliste() -> set:
    """
    Schuelerliste fuer die Schiene anlegen

    :return: A set containing the ids of all students.
    """

    global cursor
    cursor.execute("SELECT * \n FROM modulwahl")

    student_list = set()

    for student in cursor:
        student_list.add(student[0])

    return student_list


def zuteilen(source: set, students: set) -> list:
    """
    Schueler auswaehlen und zuteilen

    :param source: eine Modulliste
    :param students: freie SuS
    :return: list of students assigned to source
    """

    # SuS waehlen
    picked = []

    while len(picked) < 22:
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
            if len(picked) >= 22:
                break
            if s not in picked and s in students:
                picked.append(s)  # wenn SuS in current & kein Modul dann picked
                students.discard(s)
                source.discard(s)
        if len(source) == 0 or len(students) == 0:
            break

            # Rueckgabe der Kurslisten
    return picked


def main() -> None:
    """
    Program entry point.

    :return: Nothing
    """

    # Modul fuellen
    seed()

    try:
        connection = connect_to_db()
        run(connection=connection)
        exit(0)

    except Exception as e:
        print(f'Something broke ...\n\tReason:{str(e)}')
        exit(1)


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
            cursor.execute(command, *args)
            # commit the mysql transaction
            cursor.commit()

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


def run(connection: object) -> None:
    """
    Program main thread for building the section lists and sending them to the database for permanent storage

    :param connection: A valid MySQL connection object.
    :type connection: :class:``MySQLConnection``
    :return: Nothing
    """

    if connection is None:
        print('Invalid connection received! Terminating ...')
        exit(1)

    cursor = connection.cursor()

    # Modullisten erstellen

    deutsch = 10
    english = 10
    mathe = 10
    franz = 4
    segel = 8
    laengen = [deutsch, english, mathe, franz]
    prefix = ['D', 'E', 'M', 'F']

    modullisten = []
    segels = []

    for i, j in zip(laengen, prefix):  # zip gibt Tupel zurueck solange möglich    hier 4 Tupel
        for k in range(i):  # fuer Deu 10 Kurse, fuer E 10 Kurse usw.
            modullisten.append((modulliste('{}{}'.format(j, k + 1)), '{}{}'.format(j, k + 1)))

    for i in range(segel):
        segels.append((modulliste('S{}'.format(i + 1)), 'S{}'.format(i + 1)))

    sl = set()
    result = []

    for i in range(9):
        modullisten.sort(reverse=True)
        sl = schuelerliste()
        A = []
        for j in range(8):
            A.append((zuteilen(modullisten[j][0], sl), modullisten[j][1]))

        if i == 8:
            r = randint(0, 7)
            A.append((zuteilen(segels[r][0], sl), segels[r][1]))
        else:
            A.append((zuteilen(segels[i][0], sl), segels[i][1]))

        A.sort()
        bias = 0
        while len(sl) > 0:
            for k in range(len(A)):
                number = (22 + bias) - len(A[k][0])
                picked = sample(sl, number if len(sl) >= number else len(sl))
                A[k][0].extend(picked)
                for s in picked:
                    if s in sl:
                        sl.discard(s)
            bias += 1

        result.append(A)

    for i, A in enumerate(result):
        print("*" * 75)
        print("\nBegin Section {}: ".format(i + 1))
        seen = {}
        for modul in A:
            print(f"{modul[1]}: {modul[0]}\n\tAnzahl Schueler: {len(modul[0])}\n")
            for s in modul[0]:
                if s in seen:
                    print(f'Student {s} is in multiple modules!')
                else:
                    seen[s] = s

        print("*" * 75)

    for i, _ in enumerate(result):
        f = open(f'./RESULTS/section{i+1}.txt', 'a')
        for modul in result[i]:
            f.write(f"{modul[1]}: {modul[0]}\n\tAnzahl Schueler: {len(modul[0])}\n")
        f.close()


    cursor.close()
    connection.close()


if __name__ == '__main__':
    main()


