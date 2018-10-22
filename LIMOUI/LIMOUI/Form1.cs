using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace LIMOUI
{
    public partial class Form1 : Form
    {
        MySqlDataAdapter daStudents, daAvailableCourses, daClasses, daSchoolTypes;
        MySqlConnection limoServer;
        DataSet dsStudents, dsAvailableCourses, dsClasses, dsSchoolTypes;
        string connStr;
        int currentStudentTableIndex;
        string[] prefixes = new string[5];
        int[] laengen = new int[5];
        Dictionary<string, int> courseDict = new Dictionary<string, int>();
        Dictionary<string, int> classesDict = new Dictionary<string, int>();
        Dictionary<string, int> schoolTypesDict = new Dictionary<string, int>();
        CheckBox[] studentChoicesCheckBoxes;
        RadioButton[] assignedCoursesSelector;
        int studentInfoValidationErrors;
        bool studentInfoChanged;
        Dictionary<string, string> connectionString = new Dictionary<string, string>();
        string limoInputConnection;

        public Form1()
        {
            InitializeComponent();
            toolStripStatusLabel1.Text = "";

            StudentInfoValidationErrors = 0;
            StudentInfoChanged = false;
            AssignedCoursesSelector = new RadioButton[11];
            AssignedCoursesSelector[0] = courseSelector01;
            AssignedCoursesSelector[1] = courseSelector02;
            AssignedCoursesSelector[2] = courseSelector03;
            AssignedCoursesSelector[3] = courseSelector04;
            AssignedCoursesSelector[4] = courseSelector05;
            AssignedCoursesSelector[5] = courseSelector06;
            AssignedCoursesSelector[6] = courseSelector07;
            AssignedCoursesSelector[7] = courseSelector08;
            AssignedCoursesSelector[8] = courseSelector09;
            AssignedCoursesSelector[9] = courseSelector10;
            AssignedCoursesSelector[10] = courseSelector11;
        }

        public string ConnStr { get => connStr; set => connStr = value; }
        public MySqlConnection LimoServer { get => limoServer; set => limoServer = value; }
        public int CurrentStudentTableIndex { get => currentStudentTableIndex; set => currentStudentTableIndex = value; }
        public int[] Laengen { get => laengen; set => laengen = value; }
        public string[] Prefixes { get => prefixes; set => prefixes = value; }
        public Dictionary<string, int> CourseDict { get => courseDict; set => courseDict = value; }
        public CheckBox[] StudentChoicesCheckBoxes { get => studentChoicesCheckBoxes; set => studentChoicesCheckBoxes = value; }
        public Dictionary<string, int> SchoolTypesDict { get => schoolTypesDict; set => schoolTypesDict = value; }
        public Dictionary<string, int> ClassesDict { get => classesDict; set => classesDict = value; }
        public int StudentInfoValidationErrors { get => studentInfoValidationErrors; set => studentInfoValidationErrors = value; }
        public bool StudentInfoChanged { get => studentInfoChanged; set => studentInfoChanged = value; }
        public RadioButton[] AssignedCoursesSelector { get => assignedCoursesSelector; set => assignedCoursesSelector = value; }
        public string LimoInputConnection { get => limoInputConnection; set => limoInputConnection = value; }
        public Dictionary<string, string> ConnectionString { get => connectionString; set => connectionString = value; }

        private void NextBtn_Click(object sender, EventArgs e)
        {
            // first row in the table view is the header; it has to be excluded from
            // navigation: 
            //         number of data rows is studentTableView.Rows.Count - 1
            //         first data row is studentTableView.Rows[0]
            //         last data row is studentTableView.Rows[studentTableView.Rows.Count - 2]
            studentTableView.Rows[CurrentStudentTableIndex].Selected = false;
            ClearStudentChoices();
            CurrentStudentTableIndex += 1;
            if(CurrentStudentTableIndex >= (studentTableView.Rows.Count - 1))
            {
                CurrentStudentTableIndex = 0;
            }
            studentTableView.Rows[CurrentStudentTableIndex].Selected = true;
            GetCurrentStudent(CurrentStudentTableIndex);
            GetStudentChoices((int)studentTableView.Rows[CurrentStudentTableIndex].Cells[0].Value);
        }

        private void PrevBtn_Click(object sender, EventArgs e)
        {
            // first row in the table view is the header; it has to be excluded from
            // navigation: 
            //         number of data rows is studentTableView.Rows.Count - 1
            //         first data row is studentTableView.Rows[0]
            //         last data row is studentTableView.Rows[studentTableView.Rows.Count - 2]
            studentTableView.Rows[CurrentStudentTableIndex].Selected = false;
            ClearStudentChoices();
            CurrentStudentTableIndex -= 1;
            if (CurrentStudentTableIndex < 0)
            {
                CurrentStudentTableIndex = studentTableView.Rows.Count - 2;
            }
            studentTableView.Rows[CurrentStudentTableIndex].Selected = true;
            GetCurrentStudent(CurrentStudentTableIndex);
            GetStudentChoices((int)studentTableView.Rows[CurrentStudentTableIndex].Cells[0].Value);
        }

        private void GetStudentChoices(int nStudentId)
        {
            try
            {
                LimoServer.Open();

                // get the chosen courses for the selected student
                string query = String.Format("SELECT * FROM student_choices WHERE student_id = {0};", nStudentId);
                MySqlDataAdapter daChoices = new MySqlDataAdapter(query, LimoServer);
                DataSet dsChoices = new DataSet();

                daChoices.Fill(dsChoices, "student_choices");
                foreach (DataRow arow in dsChoices.Tables[0].Rows)
                {
                    StudentChoicesCheckBoxes[(int)arow[2] - 1].Checked = true;
                    ((string[])StudentChoicesCheckBoxes[(int)arow[2] - 1].Tag)[1] = ((int)arow[0]).ToString();
                }

                //countCoursesTxtBx.Text = dsChoices.Tables[0].Rows.Count.ToString();
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void ClearStudentChoices()
        {
            foreach (CheckBox abox in StudentChoicesCheckBoxes)
            {
                abox.Checked = false;
                ((string[])abox.Tag)[1] = "-1";
            }
        }

        private void UpdateStudentChoices( int nStudentId, bool isUpdateMode)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = LimoServer
                };

                if (isUpdateMode == true)
                {
                    // update existing choice
                    Dictionary<int,int> listRemoveId = new Dictionary<int,int>();
                    List<int> listAddId = new List<int>();

                    foreach (CheckBox abox in StudentChoicesCheckBoxes)
                    {
                        int id = -1, course_id = 0;
                        try
                        {
                            id = Convert.ToInt32(((string[])abox.Tag)[1]);
                            course_id = Convert.ToInt32(((string[])abox.Tag)[0]);
                        }
                        catch(ArgumentNullException)
                        {
                            id = -1;
                        }
                        catch (FormatException)
                        {
                            id = -1;
                        }
                        catch (OverflowException)
                        {
                            id = -1;
                        }
                        finally
                        {
                            if((id != -1) && (abox.Checked == false))
                            {
                                listRemoveId.Add(course_id, id);
                            }
                            if((id == -1) && (abox.Checked == true))
                            {
                                listAddId.Add(course_id);
                            }
                        }
                    }

                    LimoServer.Open();

                    for (int course_id = 1; course_id <= StudentChoicesCheckBoxes.Count(); course_id++)
                    {
                        int id, new_course_id;
                        if(listRemoveId.ContainsKey(course_id) == true)
                        {
                            id = listRemoveId[course_id];
                            if(listAddId.Count > 0)
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

                    while (listAddId.Count > 0)
                    {
                        int course_id = listAddId[0];
                        cmd.CommandText = string.Format("INSERT INTO student_choices (student_id, course_id) " +
                            "VALUES ('{0}', '{1}');", nStudentId, course_id);

                        cmd.ExecuteNonQuery();

                        listAddId.Remove(course_id);
                    }
                }
                else
                {
                    // insert a new choice
                    LimoServer.Open();
                    foreach (CheckBox abox in StudentChoicesCheckBoxes)
                    {
                        if (abox.Checked == true)
                        {
                            int course_id = Convert.ToInt32(((string[])abox.Tag)[0]);
                            cmd.CommandText = string.Format("INSERT INTO student_choices (student_id, course_id) " +
                                             "VALUES ('{0}', '{1}');", nStudentId, course_id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }

            ClearStudentChoices();
            GetStudentChoices(nStudentId);
        }

        private void DeleteStudentChoices(int nStudentId)
        {
            try
            {
                string query = string.Format("DELETE FROM student_choices WHERE (student_id = '{0}');", nStudentId);

                LimoServer.Open();
                MySqlCommand cmd = new MySqlCommand(query, LimoServer);
                toolStripStatusLabel1.Text = cmd.ExecuteNonQuery().ToString();
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void GetStudents()
        {
            try
            {
                LimoServer.Open();

                string query = 
                    "SELECT students.id, students.fname, students.lname, school_types.school_type_name, classes.grade, classes.class " +
                    "FROM students " +
                    "INNER JOIN classes ON students.class = classes.id " +
                    "INNER JOIN school_types ON students.school_type = school_types.id;";

                daStudents = new MySqlDataAdapter(query, LimoServer);
                dsStudents = new DataSet();
                //MySqlCommandBuilder cb = new MySqlCommandBuilder(daStudents);

                daStudents.Fill(dsStudents, "students");
                studentTableView.DataSource = dsStudents;
                studentTableView.DataMember = "students";

                studentTableView.Columns[0].HeaderText = "ID";
                studentTableView.Columns[1].HeaderText = "Name";
                studentTableView.Columns[2].HeaderText = "Nachname";
                studentTableView.Columns[3].HeaderText = "Schultyp";
                studentTableView.Columns[4].HeaderText = "Stufe";
                studentTableView.Columns[5].HeaderText = "Klasse";
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void NewBtn_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            schooltypeTxtBx.Text = "";
            gradeTxtBx.Text = "";
            classTxtBx.Text = "";

            prevBtn.Enabled = false;
            nextBtn.Enabled = false;
            updateBtn.Enabled = false;
            saveBtn.Enabled = true;

            ClearStudentChoices();
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            int nClass, nSchoolType;

            string sClassName = gradeTxtBx.Text + classTxtBx.Text;
            nClass = classesDict[sClassName];
            nSchoolType = schoolTypesDict[schooltypeTxtBx.Text];

            int nNewStudentId = InsertNewStudent(textBox2.Text, textBox3.Text, nClass, nSchoolType);

            prevBtn.Enabled = true;
            nextBtn.Enabled = true;
            updateBtn.Enabled = false;
            saveBtn.Enabled = false;

            RefreshStudentTableView(nNewStudentId);
        }

        private void NewStudentBtn_Click(object sender, EventArgs e)
        {
            modifyStudentBtn.Enabled = false;
            deleteStudentBtn.Enabled = false;
            newStudentBtn.Enabled = false;

            limoUiTabControl.SelectedTab = Modify;
            NewBtn_Click(sender, e);
        }

        private void RefreshStudentTableView(int nSelectedStudent)
        {
            try
            {
                LimoServer.Open();

                studentTableView.ClearSelection();
                dsStudents.Clear();
                studentTableView.Refresh();
                daStudents.Fill(dsStudents, "students");
                studentTableView.DataSource = dsStudents;
                studentTableView.DataMember = "students";
                studentTableView.Refresh();
                LimoServer.Close();

                if (nSelectedStudent != -1)
                {
                    foreach (DataGridViewRow arow in studentTableView.Rows)
                    {
                        if ((int)(arow.Cells[0].Value) == nSelectedStudent)
                        {
                            arow.Selected = true;
                            ClearStudentChoices();
                            GetStudentChoices(nSelectedStudent);
                            CurrentStudentTableIndex = arow.Index;
                            textBox1.Text = nSelectedStudent.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void DeleteStudentBtn_Click(object sender, EventArgs e)
        {
            int nStudentId = (int)studentTableView.Rows[CurrentStudentTableIndex].Cells[0].Value;
            try
            {
                string query = String.Format("DELETE FROM students WHERE (id = '{0}');", nStudentId);

                LimoServer.Open();
                MySqlCommand cmd = new MySqlCommand(query, LimoServer);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }

            DeleteStudentChoices(nStudentId);
            RefreshStudentTableView(-1);

            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            schooltypeTxtBx.Text = "";
            gradeTxtBx.Text = "";
            classTxtBx.Text = "";

            ClearStudentChoices();
        }

        private void Modify_Enter(object sender, EventArgs e) => textBox2.Focus();

        private void CourseSelectionChanged(object sender, EventArgs e)
        {
            int nCount = Convert.ToInt32(countCoursesTxtBx.Text);

            if (((CheckBox)sender).Checked == true)
            {
                nCount += 1;
            }
            else
            {
                nCount -= 1;
            }
            countCoursesTxtBx.Text = nCount.ToString();
            if (StudentInfoValidationErrors == 0)
            {
                updateBtn.Enabled = true;
            }
        }

        private void UpdateBtn_Click(object sender, EventArgs e)
        {
            prevBtn.Enabled = false;
            nextBtn.Enabled = false;
            updateBtn.Enabled = false;
            saveBtn.Enabled = false;

            int nStudentId = Convert.ToInt32(textBox1.Text);
            UpdateStudentChoices(nStudentId, true);

            if (StudentInfoChanged == true)
            {
                int nSchoolType = SchoolTypesDict[schooltypeTxtBx.Text];
                int nClass = ClassesDict[gradeTxtBx.Text +classTxtBx.Text];
                UpdateStudent(nStudentId, textBox2.Text, textBox3.Text, nClass, nSchoolType);

                StudentInfoChanged = false;

                RefreshStudentTableView(nStudentId);
            }

            prevBtn.Enabled = true;
            nextBtn.Enabled = true;
            updateBtn.Enabled = false;
            saveBtn.Enabled = false;
        }

        private int InsertNewStudent(string sFname, string sLname, int nClass, int nSchoolType)
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
                toolStripStatusLabel1.Text = ex.Message;
                nStudentId = -1;
            }
            finally
            {
                LimoServer.Close();
            }

            if (nStudentId != -1)
            {
                UpdateStudentChoices(nStudentId, false);
            }

            return nStudentId;
        }

        private void StudentName_Validating(object sender, CancelEventArgs e)
        {
            if(((TextBox)sender).Text.Length < 3)
            {
                toolStripStatusLabel1.Text = "Field can't be empty or shorter than 3 characters";
                e.Cancel = true;
                ((TextBox)sender).Select(0, ((TextBox)sender).Text.Length);
                if(Convert.ToInt32(((TextBox)sender).Tag) == 0)
                {
                    ((TextBox)sender).Tag = 1;
                    StudentInfoValidationErrors += 1;
                    updateBtn.Enabled = false;
                    ((TextBox)sender).ForeColor = Color.DarkRed;
                }
            }
        }

        private void StudentName_Validated(object sender, EventArgs e)
        {
            ((TextBox)sender).ForeColor = SystemColors.WindowText;
            toolStripStatusLabel1.Text = "";
            ((TextBox)sender).Tag = 0;
            if (StudentInfoValidationErrors > 0)
            {
                StudentInfoValidationErrors -= 1;
            }
            if (StudentInfoValidationErrors == 0)
            {
                updateBtn.Enabled = true;
                StudentInfoChanged = true;
            }
        }

        private void SchooltypeTxtBx_Validated(object sender, EventArgs e)
        {
            schooltypeTxtBx.ForeColor = SystemColors.WindowText;
            toolStripStatusLabel1.Text = "";
            schooltypeTxtBx.Tag = 0;
            if (StudentInfoValidationErrors > 0)
            {
                StudentInfoValidationErrors -= 1;
            }
            if (StudentInfoValidationErrors == 0)
            {
                updateBtn.Enabled = true;
                StudentInfoChanged = true;
            }
        }

        private void SchooltypeTxtBx_Validating(object sender, CancelEventArgs e)
        {
            if(SchoolTypesDict.ContainsKey(schooltypeTxtBx.Text) == false)
            {
                toolStripStatusLabel1.Text = "Enter a valid school type";
                e.Cancel = true;
                schooltypeTxtBx.Select(0, schooltypeTxtBx.Text.Length);
                if (Convert.ToInt32(schooltypeTxtBx.Tag) == 0)
                {
                    schooltypeTxtBx.Tag = 1;
                    StudentInfoValidationErrors += 1;
                    updateBtn.Enabled = false;
                    schooltypeTxtBx.ForeColor = Color.DarkRed;
                }
            }
        }

        private void GradeClassTxtBx_Validating(object sender, CancelEventArgs e)
        {
            string gradeClass = gradeTxtBx.Text + classTxtBx.Text;
            if (ClassesDict.ContainsKey(gradeClass) == false)
            {
                toolStripStatusLabel1.Text = "Enter valid values for <grade><class>";
                e.Cancel = true;
                ((TextBox)sender).Select(0, ((TextBox)sender).Text.Length);
                if (Convert.ToInt32(schooltypeTxtBx.Tag) == 0)
                {
                    ((TextBox)sender).Tag = 1;
                    StudentInfoValidationErrors += 1;
                    updateBtn.Enabled = false;
                    ((TextBox)sender).ForeColor = Color.DarkRed;
                }
            }
        }

        private void GradeClassTxtBx_Validated(object sender, EventArgs e)
        {
            ((TextBox)sender).ForeColor = SystemColors.WindowText;
            toolStripStatusLabel1.Text = "";
            ((TextBox)sender).Tag = 0;
            if (StudentInfoValidationErrors > 0)
            {
                StudentInfoValidationErrors -= 1;
            }
            if (StudentInfoValidationErrors == 0)
            {
                updateBtn.Enabled = true;
                StudentInfoChanged = true;
            }
        }

        private void TrackDateTxtBx_Validated(object sender, EventArgs e)
        {
            ((TextBox)sender).ForeColor = SystemColors.WindowText;
            toolStripStatusLabel1.Text = "";
        }

        private void TrackDateTxtBx_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int year = Convert.ToInt32(((TextBox)sender).Text);
                if((year > 2022) || (year < 2018))
                {
                    e.Cancel = true;
                }
            }
            catch (ArgumentNullException)
            {
                e.Cancel = true;
            }
            catch (FormatException)
            {
                e.Cancel = true;
            }
            catch (OverflowException)
            {
                e.Cancel = true;
            }
            finally
            {
                if (e.Cancel == true)
                {
                    toolStripStatusLabel1.Text = "Please enter a valid four digit year";
                    ((TextBox)sender).Select(0, ((TextBox)sender).Text.Length);
                    ((TextBox)sender).ForeColor = Color.DarkRed;
                }
            }
        }

        private void TrackTxtBx_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int section = Convert.ToInt32(((TextBox)sender).Text);
                if ((section > 10) || (section < 1))
                {
                    e.Cancel = true;
                }
            }
            catch (ArgumentNullException)
            {
                e.Cancel = true;
            }
            catch (FormatException)
            {
                e.Cancel = true;
            }
            catch (OverflowException)
            {
                e.Cancel = true;
            }
            finally
            {
                if (e.Cancel == true)
                {
                    toolStripStatusLabel1.Text = "Please enter a valid value: 0 < module no <= 10";
                    ((TextBox)sender).Select(0, ((TextBox)sender).Text.Length);
                    ((TextBox)sender).ForeColor = Color.DarkRed;
                }
            }
        }

        private void RefreshBtn_Click(object sender, EventArgs e)
        {
            CancelEventArgs cancelEventArgs = new CancelEventArgs(false);
            TrackDateTxtBx_Validating(trackDateTxtBx, cancelEventArgs);
            if (false == cancelEventArgs.Cancel)
            {
                TrackTxtBx_Validating(trackTxtBx, cancelEventArgs);
            }

            if (false == cancelEventArgs.Cancel)
            {
                TrackDateTxtBx_Validated(trackDateTxtBx, new EventArgs());
                TrackDateTxtBx_Validated(trackTxtBx, new EventArgs());

                int nYear = Convert.ToInt32(trackDateTxtBx.Text);
                int nTrack = Convert.ToInt32(trackTxtBx.Text);
                int i = 0;

                foreach (RadioButton rb in AssignedCoursesSelector)
                {
                    rb.Visible = false;
                    rb.Enabled = false;
                    rb.ForeColor = SystemColors.ControlText;
                }

                foreach (KeyValuePair<string, int> kvp in CourseDict)
                {
                    if (true == TestCourseidInAssignments(nYear, nTrack, kvp.Value))
                    {
                        AssignedCoursesSelector[i].Text = kvp.Key;
                        AssignedCoursesSelector[i].Visible = true;
                        AssignedCoursesSelector[i].Enabled = true;
                        i += 1;
                    }
                }

                // check for unassigned students
                if (true == TestCourseidInAssignments(nYear, nTrack, -1))
                {
                    AssignedCoursesSelector[i].Text = "UN";
                    AssignedCoursesSelector[i].Visible = true;
                    AssignedCoursesSelector[i].Enabled = true;
                }

                AssignedCoursesSelector[0].Checked = true;
                GetAssignedTrack(nYear, nTrack, CourseDict[AssignedCoursesSelector[0].Text]);

                int nStudentId = Convert.ToInt32(asignedCoursesView.Rows[1].Cells[1].Value);
                if (true == TestAssignmentIsLocked(nYear, nTrack, CourseDict[AssignedCoursesSelector[0].Text], nStudentId))
                {
                    foreach (RadioButton rb in AssignedCoursesSelector)
                    {
                        rb.ForeColor = Color.Green;
                    }
                    confirmTrackBtn.Enabled = false;
                    confirmTrackBtn.Visible = false;
                }
                else
                {
                    confirmTrackBtn.Enabled = true;
                    confirmTrackBtn.Visible = true;
                }
                asignedCoursesView.Tag = AssignedCoursesSelector[0].Text;
                asignedCoursesView.RowEnter += new DataGridViewCellEventHandler(AsignedCoursesView_CellContentClick);
                asignedCoursesView.Rows[0].Selected = true;

            }
        }

        private int GetAssignedTrack(int nYear, int nTrack, int nCourseId)
        {
            int nRowCount = 0;
            try
            {
                LimoServer.Open();

                string query = string.Format("SELECT course_assignments.id, course_assignments.student_id, students.fname, students.lname, classes.grade, classes.class " +
                    "FROM course_assignments " +
                    "INNER JOIN students ON course_assignments.student_id = students.id " +
                    "INNER JOIN classes ON students.class = classes.id " +
                    "WHERE track_date = '{0}' AND track = '{1}' AND course_id = '{2}';", nYear, nTrack, nCourseId);

                MySqlDataAdapter daAssignments = new MySqlDataAdapter(query, LimoServer);
                DataSet dsTemp = new DataSet();

                asignedCoursesView.Visible = false;
                asignedCoursesView.ClearSelection();
                asignedCoursesView.DataSource = null;
                asignedCoursesView.Refresh();
                daAssignments.Fill(dsTemp, "course_assignments");
                asignedCoursesView.DataSource = dsTemp;
                asignedCoursesView.DataMember = "course_assignments";
                asignedCoursesView.Refresh();

                asignedCoursesView.Columns[0].HeaderText = "Line";
                asignedCoursesView.Columns[1].HeaderText = "ID";
                asignedCoursesView.Columns[2].HeaderText = "Name";
                asignedCoursesView.Columns[3].HeaderText = "Nachname";
                asignedCoursesView.Columns[4].HeaderText = "Stufe";
                asignedCoursesView.Columns[5].HeaderText = "Klasse";

                asignedCoursesView.Columns[0].Visible = false;

                asignedCoursesView.Visible = true;
                nRowCount = dsTemp.Tables[0].Rows.Count;
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }

            countStudentsTxtBx.Text = nRowCount.ToString();
            return nRowCount;
        }

        private bool TestCourseidInAssignments(int nYear, int nTrack, int nCourseId)
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
                toolStripStatusLabel1.Text = ex.Message;
                bClassIsAssigned = false;
            }
            finally
            {
                LimoServer.Close();
            }

            return bClassIsAssigned;
        }

        private bool TestAssignmentIsLocked(int nYear, int nTrack, int nCourseId, int nStudentId)
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
                toolStripStatusLabel1.Text = ex.Message;
                bClassIsLocked = false;
            }
            finally
            {
                LimoServer.Close();
            }

            return bClassIsLocked;
        }

        private void CourseSelector01_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked == true)
            {
                int nYear = Convert.ToInt32(trackDateTxtBx.Text);
                int nModule = Convert.ToInt32(trackTxtBx.Text);

                if (((RadioButton)sender).Text == "UN")
                {
                    GetAssignedTrack(nYear, nModule, -1);
                    asignedCoursesView.ContextMenuStrip = assignStudentMenu;
                    courseSelectorCombo.Items.Clear();
                    foreach (RadioButton rb in AssignedCoursesSelector)
                    {
                        courseSelectorCombo.Items.Add(rb.Text);
                    }
                }
                else
                {
                    GetAssignedTrack(nYear, nModule, CourseDict[((RadioButton)sender).Text]);
                    asignedCoursesView.ContextMenuStrip = null;
                    courseSelectorCombo.Items.Clear();
                }
                asignedCoursesView.Tag = ((RadioButton)sender).Text;
            }
        }

        private void AssignStudentMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            assignStudentMenu.Hide();
            if (courseSelectorCombo.SelectedItem != null)
            {
                if (asignedCoursesView.SelectedRows.Count == 1)
                {
                    DataGridViewRow dgvSelectedRow = asignedCoursesView.SelectedRows[0];
                    int nYear = Convert.ToInt32(trackDateTxtBx.Text);
                    int nModule = Convert.ToInt32(trackTxtBx.Text);

                    UpdateStudentAssignment(
                        CourseDict[courseSelectorCombo.SelectedItem.ToString()],
                        Convert.ToInt32(dgvSelectedRow.Cells[0].Value));

                    if (TestCourseidInAssignments(nYear, nModule, -1) == false)
                    {
                        foreach (RadioButton rb in AssignedCoursesSelector)
                        {
                            if (rb.Text == "UN")
                            {
                                rb.Enabled = false;
                                rb.Visible = false;
                                AssignedCoursesSelector[0].Checked = true;
                            }
                        }

                    }
                    else
                    {
                        GetAssignedTrack(nYear, nModule, -1);
                    }
                }
            }
        }

        private void UpdateStudentAssignment(int nNewClass, int nLineId)
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
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void ConfirmTrack(int nYear, int nTrack)
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
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void ConfirmTrackBtn_Click(object sender, EventArgs e)
        {
            ((Button)sender).Enabled = false;
            ((Button)sender).Visible = false;

            ConfirmTrack(Convert.ToInt32(trackDateTxtBx.Text), Convert.ToInt32(trackTxtBx.Text));
        }

        private void GetAllCoursesForStudent(int nStudentId, int nYear)
        {
            try
            {
                LimoServer.Open();
                string query = string.Format("SELECT course_assignments.track, available_courses.type, available_courses.num " +
                    "FROM course_assignments " +
                    "INNER JOIN available_courses ON available_courses.id = course_assignments.course_id " +
                    "WHERE track_date = '{0}' AND student_id = '{1}';", nYear, nStudentId);

                MySqlDataAdapter daAssignments = new MySqlDataAdapter(query, LimoServer);
                DataSet dsTemp = new DataSet();

                studentDetailView.Visible = false;
                studentDetailView.ClearSelection();
                studentDetailView.DataSource = null;
                studentDetailView.Refresh();

                daAssignments.Fill(dsTemp, "course_assignments");
                studentDetailView.DataSource = dsTemp;
                studentDetailView.DataMember = "course_assignments";
                studentDetailView.Refresh();

                studentDetailView.Columns[0].HeaderText = "Track";
                studentDetailView.Columns[1].HeaderText = "Course";

                studentDetailView.Visible = true;
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void AsignedCoursesView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                asignedCoursesView.Rows[e.RowIndex].Selected = true;
                studentDetailNameTxtBx.Text =
                    asignedCoursesView.Rows[e.RowIndex].Cells[2].Value.ToString() +
                    " " +
                    asignedCoursesView.Rows[e.RowIndex].Cells[3].Value.ToString();

                GetAllCoursesForStudent(
                    Convert.ToInt32(asignedCoursesView.Rows[e.RowIndex].Cells[1].Value),
                    Convert.ToInt32(trackDateTxtBx.Text));
            }
        }

        private void UpdateStudent(int nStudentId, string sFname, string sLname, int nClass, int nSchoolType)
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
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void GetAvailableCoursesClassesAndSchooltypes()
        {
            try
            {
                LimoServer.Open();

                // read the table available_courses into a dataset
                string query = "SELECT * FROM available_courses ORDER BY id;";
                daAvailableCourses = new MySqlDataAdapter(query, LimoServer);

                dsAvailableCourses = new DataSet();
                daAvailableCourses.Fill(dsAvailableCourses, "available_courses");

                // read the table classes into a dataset
                query = "SELECT * FROM classes ORDER BY id;";
                daClasses = new MySqlDataAdapter(query, LimoServer);

                dsClasses = new DataSet();
                daClasses.Fill(dsClasses, "classes");

                // read the table school_types into a dataset
                query = "SELECT * FROM school_types ORDER BY id;";
                daSchoolTypes = new MySqlDataAdapter(query, LimoServer);

                dsSchoolTypes = new DataSet();
                daSchoolTypes.Fill(dsSchoolTypes, "school_types");


                // extract course types and number of courses from dataset
                string last_prefix = dsAvailableCourses.Tables[0].Rows[0][1].ToString();

                int last_length = 0;
                int courseGroups = 0;
                int totalCourses = 0;

                foreach (DataRow acourse in dsAvailableCourses.Tables[0].Rows)
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

                label17.Text = Prefixes[0];
                label18.Text = Prefixes[1];
                label19.Text = Prefixes[2];
                label20.Text = Prefixes[3];
                label21.Text = Prefixes[4];

                // create table array with checkboxes to view/select student course choices
                StudentChoicesCheckBoxes = new CheckBox[totalCourses];
                int[] location_X = { 3, 75, 147, 219, 291 };
                int[] location_Y = { 23, 57, 91, 125, 159, 193, 227, 261, 295, 329 };
                int courseId = 0;

                for (int column = 0; column < courseGroups; column++)
                {
                    for (int row = 0; row < Laengen[column]; row++)
                    {
                        CheckBox temp = new CheckBox
                        {
                            AutoSize = true,
                            Location = new System.Drawing.Point(location_X[column], location_Y[row]),
                            Name = String.Format("checkBox{0}{1}", column, row),
                            Size = new System.Drawing.Size(15, 14),
                            TabIndex = 5 + courseId,
                            // we use 'Tag' to store the course ID (->table available_courses)
                            // and later the id from the student_choices table so that we can 
                            // easily update lines or insert a new line
                            Tag = new string[2]
                            {
                                (courseId + 1).ToString(),
                                "-1"
                            },
                            Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top),
                            Padding = new Padding(3),
                            //Enabled = false,
                            UseVisualStyleBackColor = true
                        };

                        temp.CheckedChanged += new EventHandler(CourseSelectionChanged);

                        StudentChoicesCheckBoxes[courseId++] = temp;
                        tableLayoutPanel1.Controls.Add(temp, column, row + 1);
                    }
                }

                // create classes dictionary
                foreach (DataRow arow in dsClasses.Tables[0].Rows)
                {
                    string class_name = arow[1].ToString() + arow[2].ToString();
                    ClassesDict.Add(class_name, (int)arow[0]);
                }

                // create school_types dictionary
                foreach (DataRow arow in dsSchoolTypes.Tables[0].Rows)
                {
                    SchoolTypesDict.Add(arow[1].ToString(), (int)arow[0]);
                }

            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
            finally
            {
                LimoServer.Close();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            LimoInputConnection = Environment.GetEnvironmentVariable("limoinput_connection");
            if (LimoInputConnection == null)
            {
                toolStripStatusLabel1.Text = "Environment Variable limoinput_connection must be defined.";
            }
            else
            {
                LimoInputConnection = LimoInputConnection.Trim(new char[] { '{', '}', '\"' });
                LimoInputConnection = LimoInputConnection.Replace('\"', ' ');

                String[] elements = LimoInputConnection.Split(new char[] {':', ','});
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
                    ConnStr = ConnStr.TrimEnd(new char[] { ';'});

                    LimoServer = new MySqlConnection(ConnStr);

                    GetStudents();
                    GetAvailableCoursesClassesAndSchooltypes();

                    studentTableView.RowEnter += new DataGridViewCellEventHandler(StudentTableView_RowEnter);
                    studentTableView.Rows[0].Selected = true;
                    countCoursesTxtBx.Text = "0";
                }
                else
                {
                    toolStripStatusLabel1.Text = "Environment Variable limoinput_connection has wrong format.";
                }
            }
        }

        private void GetCurrentStudent(int rowIndex)
        {
            toolStripStatusLabel1.Text = "";
            textBox1.Text = studentTableView.Rows[rowIndex].Cells[0].Value.ToString();
            textBox2.Text = studentTableView.Rows[rowIndex].Cells[1].Value.ToString();
            textBox3.Text = studentTableView.Rows[rowIndex].Cells[2].Value.ToString();
            schooltypeTxtBx.Text = studentTableView.Rows[rowIndex].Cells[3].Value.ToString();
            gradeTxtBx.Text = studentTableView.Rows[rowIndex].Cells[4].Value.ToString();
            classTxtBx.Text = studentTableView.Rows[rowIndex].Cells[5].Value.ToString();
        }

        private void StudentTableView_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            studentTableView.Rows[e.RowIndex].Selected = true;
            CurrentStudentTableIndex = e.RowIndex;
            modifyStudentBtn.Enabled = true;
            deleteStudentBtn.Enabled = true;
            newStudentBtn.Enabled = true;
        }

        private void ModifyStudentBtn_Click(object sender, EventArgs e)
        {
            modifyStudentBtn.Enabled = false;
            deleteStudentBtn.Enabled = false;
            newStudentBtn.Enabled = false;
            limoUiTabControl.SelectedTab = Modify;
            ClearStudentChoices();
            GetCurrentStudent(CurrentStudentTableIndex);
            GetStudentChoices((int)studentTableView.Rows[CurrentStudentTableIndex].Cells[0].Value);

            prevBtn.Enabled = true;
            nextBtn.Enabled = true;
            updateBtn.Enabled = false;
            saveBtn.Enabled = false;

        }
    }
}
