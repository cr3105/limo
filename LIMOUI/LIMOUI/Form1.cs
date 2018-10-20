using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        int moduleSelectionValidationErrors;
        bool studentInfoChanged;

        public Form1()
        {
            InitializeComponent();
            toolStripStatusLabel1.Text = "";
            StudentInfoValidationErrors = 0;
            StudentInfoChanged = false;
            ModuleSelectionValidationErrors = 0;
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
        public int ModuleSelectionValidationErrors { get => moduleSelectionValidationErrors; set => moduleSelectionValidationErrors = value; }
        public RadioButton[] AssignedCoursesSelector { get => assignedCoursesSelector; set => assignedCoursesSelector = value; }

        private void NextBtn_Click(object sender, EventArgs e)
        {
            // first row in the table view is the header; it has to be excluded from
            // navigation: 
            //         number of data rows is studentTableView.Rows.Count - 1
            //         first data row is studentTableView.Rows[0]
            //         last data row is studentTableView.Rows[studentTableView.Rows.Count - 2]
            studentTableView.Rows[currentStudentTableIndex].Selected = false;
            ClearStudentChoices();
            currentStudentTableIndex += 1;
            if(currentStudentTableIndex >= (studentTableView.Rows.Count - 1))
            {
                currentStudentTableIndex = 0;
            }
            studentTableView.Rows[currentStudentTableIndex].Selected = true;
            GetCurrentStudent(currentStudentTableIndex);
            GetStudentChoices((int)studentTableView.Rows[currentStudentTableIndex].Cells[0].Value);
        }

        private void PrevBtn_Click(object sender, EventArgs e)
        {
            // first row in the table view is the header; it has to be excluded from
            // navigation: 
            //         number of data rows is studentTableView.Rows.Count - 1
            //         first data row is studentTableView.Rows[0]
            //         last data row is studentTableView.Rows[studentTableView.Rows.Count - 2]
            studentTableView.Rows[currentStudentTableIndex].Selected = false;
            ClearStudentChoices();
            currentStudentTableIndex -= 1;
            if (currentStudentTableIndex < 0)
            {
                currentStudentTableIndex = studentTableView.Rows.Count - 2;
            }
            studentTableView.Rows[currentStudentTableIndex].Selected = true;
            GetCurrentStudent(currentStudentTableIndex);
            GetStudentChoices((int)studentTableView.Rows[currentStudentTableIndex].Cells[0].Value);
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
                            currentStudentTableIndex = arow.Index;
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
            int nStudentId = (int)studentTableView.Rows[currentStudentTableIndex].Cells[0].Value;
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

        private void SectionDateTxtBx_Validated(object sender, EventArgs e)
        {
            ((TextBox)sender).ForeColor = SystemColors.WindowText;
            toolStripStatusLabel1.Text = "";
            ((TextBox)sender).Tag = 0;
            if (ModuleSelectionValidationErrors > 0)
            {
                ModuleSelectionValidationErrors -= 1;
            }
            if (ModuleSelectionValidationErrors == 0)
            {
                refreshBtn.Enabled = true;
            }
        }

        private void SectionDateTxtBx_Validating(object sender, CancelEventArgs e)
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
                    if (Convert.ToInt32(((TextBox)sender).Tag) == 0)
                    {
                        ((TextBox)sender).Tag = 1;
                        ModuleSelectionValidationErrors += 1;
                        refreshBtn.Enabled = false;
                        ((TextBox)sender).ForeColor = Color.DarkRed;
                    }
                }
            }
        }

        private void SectionTxtBx_Validating(object sender, CancelEventArgs e)
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
                    if (Convert.ToInt32(((TextBox)sender).Tag) == 0)
                    {
                        ((TextBox)sender).Tag = 1;
                        ModuleSelectionValidationErrors += 1;
                        refreshBtn.Enabled = false;
                        ((TextBox)sender).ForeColor = Color.DarkRed;
                    }
                }
            }
        }

        private void RefreshBtn_Click(object sender, EventArgs e)
        {
            refreshBtn.Enabled = false;
            refreshBtn.Refresh();

            int nYear = Convert.ToInt32(sectionDateTxtBx.Text);
            int nModule = Convert.ToInt32(sectionTxtBx.Text);
            int i = 0;

            foreach (RadioButton rb in AssignedCoursesSelector)
            {
                rb.Visible = false;
            }

            foreach (KeyValuePair<string, int> kvp in CourseDict)
            {
                if (true == TestClassidInAssignments(nYear, nModule, kvp.Value))
                {
                    AssignedCoursesSelector[i].Text = kvp.Key;
                    AssignedCoursesSelector[i].Visible = true;
                    i += 1;
                }
            }

            AssignedCoursesSelector[0].Checked = true;
            GetAssignedModule(nYear, nModule, CourseDict[AssignedCoursesSelector[0].Text]);

        }

        private void GetAssignedModule(int nYear, int nModule, int nClassId)
        {
            try
            {
                LimoServer.Open();

                string query =
                    "SELECT course_assignments.student_id, students.fname, students.lname, classes.grade, classes.class " +
                    "FROM course_assignments " +
                    "INNER JOIN classes ON students.class = classes.id " +
                    "INNER JOIN students ON course_assignments.student_id = students.id " +
                    "WHERE section_date = '{0}' AND section = '{1}' AND class_id = '{2}';";

                MySqlDataAdapter daAssignments = new MySqlDataAdapter(query, LimoServer);
                DataSet dsTemp = new DataSet();

                daAssignments.Fill(dsTemp, "course_assignments");
                asignedCoursesView.DataSource = dsTemp;
                asignedCoursesView.DataMember = "course_assignments";

                asignedCoursesView.Columns[0].HeaderText = "ID";
                asignedCoursesView.Columns[1].HeaderText = "Name";
                asignedCoursesView.Columns[2].HeaderText = "Nachname";
                asignedCoursesView.Columns[3].HeaderText = "Stufe";
                asignedCoursesView.Columns[4].HeaderText = "Klasse";

                asignedCoursesView.Visible = true;
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

        private bool TestClassidInAssignments(int nYear, int nModule, int nClassId)
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
                    "WHERE section_date = '{0}' AND section = '{1}' AND class_id = '{2}';",
                    nYear, nModule, nClassId);
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
            //ConnStr = "server=cdr-wa.dyndns.org;port=3105;database=david_db;user=david;password=51Jti0IeQn";
            ConnStr = "server=localhost;port=3306;database=david_db;user=cr3105;password=David#3105";
            LimoServer = new MySqlConnection(ConnStr);

            GetStudents();
            GetAvailableCoursesClassesAndSchooltypes();

            studentTableView.RowEnter += new DataGridViewCellEventHandler(StudentTableView_RowEnter);
            studentTableView.Rows[0].Selected = true;
            countCoursesTxtBx.Text = "0";
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
            currentStudentTableIndex = e.RowIndex;
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
            GetCurrentStudent(currentStudentTableIndex);
            GetStudentChoices((int)studentTableView.Rows[currentStudentTableIndex].Cells[0].Value);

            prevBtn.Enabled = true;
            nextBtn.Enabled = true;
            updateBtn.Enabled = false;
            saveBtn.Enabled = false;

        }
    }
}
