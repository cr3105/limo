using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace DBAccess
{
    [Serializable()]
    public class DBAccessException : Exception
    {
        private readonly int errorCode;

        protected DBAccessException()
              : base()
        { }

        public DBAccessException(int value) :
           base(String.Format("{0}: Database operation failed.", value))
        {
            errorCode = value;
        }

        public DBAccessException(int value, string message)
           : base(message)
        {
            errorCode = value;
        }

        public DBAccessException(int value, string message, Exception innerException) :
           base(message, innerException)
        {
            errorCode = value;
        }

        protected DBAccessException(SerializationInfo info,
                                    StreamingContext context)
           : base(info, context)
        { }

        public int ErrorCode
        { get { return errorCode; } }
    }

    public class MySqlAccess
    {
        MySqlConnection limoServer;
        DataSet dsAvailableCourses, dsClasses, dsSchoolTypes;
        string connStr;
        string[] prefixes = new string[5];
        Dictionary<string, int> courseDict = new Dictionary<string, int>();
        Dictionary<string, int> classesDict = new Dictionary<string, int>();
        Dictionary<string, int> schoolTypesDict = new Dictionary<string, int>();
        Dictionary<string, string> connectionString = new Dictionary<string, string>();
        int courseGroups;
        int totalCourses;
        int[] laengen = new int[5];

        /// <summary>
        /// Information to create the connection string for the Limo database.
        /// </summary>
        public string LimoInputConnection => Environment.GetEnvironmentVariable("limoinput_connection");

        private string ConnStr { get => connStr; set => connStr = value; }
        private MySqlConnection LimoServer { get => limoServer; set => limoServer = value; }
        private Dictionary<string, string> ConnectionString { get => connectionString; set => connectionString = value; }

        public int[] Laengen { get => laengen; set => laengen = value; }
        public string[] Prefixes { get => prefixes; set => prefixes = value; }
        public Dictionary<string, int> CourseDict { get => courseDict; set => courseDict = value; }
        public Dictionary<string, int> SchoolTypesDict { get => schoolTypesDict; set => schoolTypesDict = value; }
        public Dictionary<string, int> ClassesDict { get => classesDict; set => classesDict = value; }
        public int TotalCourses { get => totalCourses; set => totalCourses = value; }
        public int CourseGroups { get => courseGroups; set => courseGroups = value; }
        public DataSet DsAvailableCourses { get => dsAvailableCourses; set => dsAvailableCourses = value; }
        public DataSet DsClasses { get => dsClasses; set => dsClasses = value; }
        public DataSet DsSchoolTypes { get => dsSchoolTypes; set => dsSchoolTypes = value; }

        public bool InitializeDbConnection()
        {
            bool bSuccess = false;

            string connection = LimoInputConnection.Trim(new char[] { '{', '}', '\"' });
            connection = connection.Replace('\"', ' ');

            String[] elements = connection.Split(new char[] { ':', ',' });
            if ((elements.Length % 2) == 0)
            {
                for (int i = 0; i < elements.Length; i += 2)
                {
                    ConnectionString[elements[i].Trim()] = elements[i + 1].Trim();
                }

                ConnStr = "";
                if (ConnectionString.ContainsKey("host"))
                {
                    ConnStr += "server=" + ConnectionString["host"] + ";";
                }
                if (ConnectionString.ContainsKey("port"))
                {
                    ConnStr += "port=" + ConnectionString["port"] + ";";
                }
                if (ConnectionString.ContainsKey("db"))
                {
                    ConnStr += "database=" + ConnectionString["db"] + ";";
                }
                if (ConnectionString.ContainsKey("user"))
                {
                    ConnStr += "user=" + ConnectionString["user"] + ";";
                }
                if (ConnectionString.ContainsKey("passwd"))
                {
                    ConnStr += "password=" + ConnectionString["passwd"] + ";";
                }
                ConnStr = ConnStr.TrimEnd(new char[] { ';' });

                LimoServer = new MySqlConnection(ConnStr);

                bSuccess = true;
            }

            return bSuccess;
        }

        public DataSet GetStudentChoices(int nStudentId)
        {
            DataSet dsChoices = new DataSet();
            try
            {
                LimoServer.Open();

                // get the chosen courses for the selected student
                string query = String.Format("SELECT * FROM student_choices WHERE student_id = {0};", nStudentId);
                MySqlDataAdapter daChoices = new MySqlDataAdapter(query, LimoServer);

                daChoices.Fill(dsChoices, "student_choices");
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1004, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
            return dsChoices;
        }

        public void DeleteStudentChoices(int nStudentId)
        {
            try
            {
                string query = string.Format("DELETE FROM student_choices WHERE (student_id = '{0}');", nStudentId);

                LimoServer.Open();
                MySqlCommand cmd = new MySqlCommand(query, LimoServer);
                cmd.ExecuteNonQuery().ToString();
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1005, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
        }

        public void InsertStudentChoices(List<int> listAddId, int nStudentId)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };
                LimoServer.Open();

                while (listAddId.Count > 0)
                {
                    int course_id = listAddId[0];
                    cmd.CommandText = string.Format("INSERT INTO student_choices (student_id, course_id) " +
                                        "VALUES ('{0}', '{1}');", nStudentId, course_id);
                    cmd.ExecuteNonQuery();

                    listAddId.Remove(course_id);
                }

            }
            catch (Exception ex)
            {
                throw new DBAccessException(1006, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
        }

        public void UpdateStudentChoices(Dictionary<int, int> listRemoveId, List<int> listAddId, int nMaxCourses, int nStudentId)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };
                LimoServer.Open();

                for (int course_id = 1; course_id <= nMaxCourses; course_id++)
                {
                    int id, new_course_id;
                    if (listRemoveId.ContainsKey(course_id) == true)
                    {
                        id = listRemoveId[course_id];
                        if (listAddId.Count > 0)
                        {
                            new_course_id = listAddId[0];
                            cmd.CommandText = string.Format("UPDATE student_choices " +
                                "SET course_id = '{0}' WHERE(id = '{1}');", new_course_id, id);
                            cmd.ExecuteNonQuery();
                            listAddId.Remove(new_course_id);
                            listRemoveId.Remove(course_id);
                        }
                        else
                        {
                            cmd.CommandText = string.Format("DELETE FROM student_choices " +
                                "WHERE(id = '{0}');", id);
                            cmd.ExecuteNonQuery();
                            listRemoveId.Remove(course_id);
                        }
                    }
                    if (listRemoveId.Count == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1007, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            if (listAddId.Count > 0)
            {
                InsertStudentChoices(listAddId, nStudentId);
            }
        }

        public DataSet GetStudentTable()
        {
            DataSet dsStudents = new DataSet();

            try
            {
                LimoServer.Open();
                string query =
                    "SELECT students.id, students.fname, students.lname, school_types.school_type_name, classes.grade, classes.class " +
                    "FROM students " +
                    "INNER JOIN classes ON students.class = classes.id " +
                    "INNER JOIN school_types ON students.school_type = school_types.id;";

                MySqlDataAdapter daStudents = new MySqlDataAdapter(query, LimoServer);
                daStudents.Fill(dsStudents, "students");
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1008, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return dsStudents;
        }

        public int InsertStudent(string sFname, string sLname, int nClass, int nSchoolType)
        {
            int nStudentId = -1;
            try
            {
                string query = string.Format("INSERT INTO students (fname, lname, class, school_type) " +
                    "VALUES ('{0}', '{1}', '{2}', '{3}');", sFname, sLname, nClass, nSchoolType);

                LimoServer.Open();
                MySqlCommand cmd = new MySqlCommand(query, LimoServer);
                cmd.ExecuteNonQuery();

                cmd.CommandText = string.Format("SELECT id FROM students " +
                    "WHERE fname = '{0}' AND lname = '{1}' AND class = '{2}' AND school_type = '{3}'",
                    sFname, sLname, nClass, nSchoolType);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    nStudentId = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                nStudentId = -1;
                throw new DBAccessException(1009, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return nStudentId;
        }

        public void DeleteStudent(int nStudentId)
        {
            try
            {
                string query = String.Format("DELETE FROM students WHERE (id = '{0}');", nStudentId);

                LimoServer.Open();
                MySqlCommand cmd = new MySqlCommand(query, LimoServer);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1010, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            DeleteStudentChoices(nStudentId);
        }

        public bool TestCourseidInAssignments(int nYear, int nTrack, int nCourseId)
        {
            bool bClassIsAssigned = false;
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };

                LimoServer.Open();
                cmd.CommandText = string.Format("SELECT COUNT(*) FROM course_assignments " +
                    "WHERE track_date = '{0}' AND track = '{1}' AND course_id = '{2}';",
                    nYear, nTrack, nCourseId);
                object result = cmd.ExecuteScalar();
                if ((result != null) && (Convert.ToInt32(result) > 0))
                {
                    bClassIsAssigned = true;
                }
            }
            catch (Exception ex)
            {
               bClassIsAssigned = false;
               throw new DBAccessException(1011, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return bClassIsAssigned;
        }

        public bool TestAssignmentIsLocked(int nYear, int nTrack, int nCourseId, int nStudentId)
        {
            bool bClassIsLocked = false;
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };

                LimoServer.Open();
                cmd.CommandText = string.Format("SELECT isLocked FROM course_assignments " +
                    "WHERE track_date = '{0}' AND track = '{1}' AND course_id = '{2}' AND student_id = {3};",
                    nYear, nTrack, nCourseId, nStudentId);
                object result = cmd.ExecuteScalar();
                if ((result != null) && (Convert.ToInt32(result) > 0))
                {
                    bClassIsLocked = true;
                }
            }
            catch (Exception ex)
            {
                bClassIsLocked = false;
                throw new DBAccessException(1012, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return bClassIsLocked;
        }

        public void UpdateStudentAssignment(int nNewClass, int nLineId)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };
                LimoServer.Open();

                cmd.CommandText = string.Format("UPDATE course_assignments SET course_id = '{0}' WHERE (id = '{1}');",
                   nNewClass, nLineId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1013, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
        }

        public void ConfirmTrack(int nYear, int nTrack)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };
                LimoServer.Open();

                cmd.CommandText = string.Format("UPDATE course_assignments SET isLocked = '1' WHERE (track_date = '{0}' AND track = '{1}');",
                   nYear, nTrack);
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format("INSERT INTO confirmed_modules (track, track_date) " +
                    "VALUES ('{0}', '{1}');", nTrack, nYear);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1014, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
        }

        public List<int> CheckStudentChoicesIntegrity()
        {
            List<int> zombies = new List<int>();
            try
            {
                LimoServer.Open();
                string query = "SELECT * FROM students RIGHT JOIN student_choices ON students.id = student_choices.student_id;";

                MySqlDataAdapter daTemp = new MySqlDataAdapter(query, LimoServer);
                DataSet dsTemp = new DataSet();

                daTemp.Fill(dsTemp, "student_choices");

                foreach (DataRow arow in dsTemp.Tables[0].Rows)
                {
                    if (arow[0].GetType() != arow[6].GetType())
                    {
                        if (zombies.Contains((int)arow[6]) == false)
                        {
                            zombies.Add((int)arow[6]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1015, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return zombies.Count > 0 ? zombies : null;
        }

        public DataSet GetTrackFromAssignments(int nYear, int nTrack, int nCourseId)
        {
            DataSet dsTemp = new DataSet();
            try
            {
                LimoServer.Open();

                string query = string.Format("SELECT course_assignments.id, course_assignments.student_id, students.fname, students.lname, classes.grade, classes.class " +
                    "FROM course_assignments " +
                    "INNER JOIN students ON course_assignments.student_id = students.id " +
                    "INNER JOIN classes ON students.class = classes.id " +
                    "WHERE track_date = '{0}' AND track = '{1}' AND course_id = '{2}';", nYear, nTrack, nCourseId);

                MySqlDataAdapter daAssignments = new MySqlDataAdapter(query, LimoServer);

                daAssignments.Fill(dsTemp, "course_assignments");
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1016, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return dsTemp;
        }

        public DataSet GetAllCoursesForStudent(int nStudentId, int nYear)
        {
            DataSet dsTemp = new DataSet();

            try
            {
                LimoServer.Open();
                string query = string.Format("SELECT course_assignments.track, available_courses.type, available_courses.num " +
                    "FROM course_assignments " +
                    "INNER JOIN available_courses ON available_courses.id = course_assignments.course_id " +
                    "WHERE track_date = '{0}' AND student_id = '{1}';", nYear, nStudentId);

                MySqlDataAdapter daAssignments = new MySqlDataAdapter(query, LimoServer);
                daAssignments.Fill(dsTemp, "course_assignments");
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1017, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return dsTemp;
        }

        public DataSet GetCourse(int nCourseID)
        {
            DataSet dsTemp = new DataSet();
            try
            {
                LimoServer.Open();
                string query = string.Format("SELECT student_choices.id, students.fname, students.lname " +
                    "FROM student_choices " +
                    "INNER JOIN students ON student_choices.student_id = students.id " +
                    "WHERE course_id = '{0}';", nCourseID);

                MySqlDataAdapter daTemp = new MySqlDataAdapter(query, LimoServer);
                daTemp.Fill(dsTemp, "student_choices");
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1002, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }

            return dsTemp;
        }

        public void UpdateStudent(int nStudentId, string sFname, string sLname, int nClass, int nSchoolType)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };
                LimoServer.Open();

                cmd.CommandText = string.Format("UPDATE students SET fname = '{0}', lname = '{1}', class = '{2}', school_type = '{3}' " +
                    "WHERE(id = '{4}');", sFname, sLname, nClass, nSchoolType, nStudentId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new DBAccessException(1001, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
        }

        public void GetAvailableCoursesClassesAndSchooltypes()
        {
            try
            {
                MySqlDataAdapter daAvailableCourses, daClasses, daSchoolTypes;
                LimoServer.Open();

                // read the table available_courses into a dataset
                string query = "SELECT * FROM available_courses ORDER BY id;";
                daAvailableCourses = new MySqlDataAdapter(query, LimoServer);

                DsAvailableCourses = new DataSet();
                daAvailableCourses.Fill(DsAvailableCourses, "available_courses");

                // read the table classes into a dataset
                query = "SELECT * FROM classes ORDER BY id;";
                daClasses = new MySqlDataAdapter(query, LimoServer);

                DsClasses = new DataSet();
                daClasses.Fill(DsClasses, "classes");

                // read the table school_types into a dataset
                query = "SELECT * FROM school_types ORDER BY id;";
                daSchoolTypes = new MySqlDataAdapter(query, LimoServer);

                DsSchoolTypes = new DataSet();
                daSchoolTypes.Fill(DsSchoolTypes, "school_types");

                // extract course types and number of courses from dataset
                string last_prefix = DsAvailableCourses.Tables[0].Rows[0][1].ToString();

                int last_length = 0;
                courseGroups = 0;
                totalCourses = 0;

                foreach (DataRow acourse in DsAvailableCourses.Tables[0].Rows)
                {
                    string course_name = acourse[1].ToString() + acourse[2].ToString();
                    CourseDict.Add(course_name, (int)acourse[0]);
                    if (last_prefix == acourse[1].ToString())
                    {
                        last_length += 1;
                    }
                    else
                    {
                        Prefixes[courseGroups] = last_prefix;
                        Laengen[courseGroups] = last_length;
                        totalCourses += last_length;
                        last_prefix = acourse[1].ToString();
                        last_length = 1;
                        courseGroups += 1;
                    }
                }
                Prefixes[courseGroups] = last_prefix;
                Laengen[courseGroups] = last_length;
                totalCourses += last_length;
                courseGroups += 1;

                // create classes dictionary
                foreach (DataRow arow in DsClasses.Tables[0].Rows)
                {
                    string class_name = arow[1].ToString() + arow[2].ToString();
                    ClassesDict.Add(class_name, (int)arow[0]);
                }

                // create school_types dictionary
                foreach (DataRow arow in DsSchoolTypes.Tables[0].Rows)
                {
                    SchoolTypesDict.Add(arow[1].ToString(), (int)arow[0]);
                }

            }
            catch (Exception ex)
            {
                throw new DBAccessException(1003, ex.Message);
            }
            finally
            {
                LimoServer.Close();
            }
        }

    }
}
