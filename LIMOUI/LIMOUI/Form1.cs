using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using DBAccess;

namespace LIMOUI
{
    public partial class Form1 : Form
    {
        CheckBox[] studentChoicesCheckBoxes;
        RadioButton[] assignedCoursesSelector;
        int studentInfoValidationErrors;
        int currentStudentTableIndex;
        bool studentInfoChanged;

        MySqlAccess mySqlAccess = new MySqlAccess();

        ErrorProvider UserInputErrorProvider;

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

            UserInputErrorProvider = new ErrorProvider
            {
                BlinkRate = 250,
                BlinkStyle = ErrorBlinkStyle.AlwaysBlink
            };
        }

        public CheckBox[] StudentChoicesCheckBoxes { get => studentChoicesCheckBoxes; set => studentChoicesCheckBoxes = value; }
        public RadioButton[] AssignedCoursesSelector { get => assignedCoursesSelector; set => assignedCoursesSelector = value; }
        public int StudentInfoValidationErrors { get => studentInfoValidationErrors; set => studentInfoValidationErrors = value; }
        public int CurrentStudentTableIndex { get => currentStudentTableIndex; set => currentStudentTableIndex = value; }
        public bool StudentInfoChanged { get => studentInfoChanged; set => studentInfoChanged = value; }

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

        private void NewBtn_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            schooltypeTxtBx.Text = "";
            gradeClassTxtBx.Text = "";

            prevBtn.Enabled = false;
            nextBtn.Enabled = false;
            updateBtn.Enabled = false;
            saveBtn.Enabled = true;

            ClearStudentChoices();
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            int nClass, nSchoolType;

            nClass = mySqlAccess.ClassesDict[gradeClassTxtBx.Text];
            nSchoolType = mySqlAccess.SchoolTypesDict[schooltypeTxtBx.Text];

            int nNewStudentId = mySqlAccess.InsertStudent(textBox2.Text, textBox3.Text, nClass, nSchoolType);
            if (nNewStudentId != -1)
            {
                UpdateStudentChoices(nNewStudentId, false);
            }

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

        private void DeleteStudentBtn_Click(object sender, EventArgs e)
        {
            int nStudentId = (int)studentTableView.Rows[CurrentStudentTableIndex].Cells[0].Value;
            mySqlAccess.DeleteStudent(nStudentId);

            RefreshStudentTableView(-1);

            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            schooltypeTxtBx.Text = "";
            gradeClassTxtBx.Text = "";

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
                int nSchoolType = mySqlAccess.SchoolTypesDict[schooltypeTxtBx.Text];
                int nClass = mySqlAccess.ClassesDict[gradeClassTxtBx.Text];
                mySqlAccess.UpdateStudent(nStudentId, textBox2.Text, textBox3.Text, nClass, nSchoolType);

                StudentInfoChanged = false;

                RefreshStudentTableView(nStudentId);
            }

            prevBtn.Enabled = true;
            nextBtn.Enabled = true;
            updateBtn.Enabled = false;
            saveBtn.Enabled = false;
        }

        private void ConfirmTrackBtn_Click(object sender, EventArgs e)
        {
            ((Button)sender).Enabled = false;
            ((Button)sender).Visible = false;

            mySqlAccess.ConfirmTrack(Convert.ToInt32(trackDateTxtBx.Text), Convert.ToInt32(trackTxtBx.Text));
        }

        private void CheckIntegrityBtn_Click(object sender, EventArgs e)
        {
            List<int> zombies = mySqlAccess.CheckStudentChoicesIntegrity();

            if (zombies != null)
            {
                string message = "Do you want to delete abandoned student choices?(";

                foreach (int id in zombies)
                {
                    message += string.Format("{0},", id);
                }

                message.TrimEnd(new char[] { ',' });
                message += ")";

                if (DialogResult.Yes ==
                    MessageBox.Show(message,
                                    "Database Integrity",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question))
                {
                    foreach (int id in zombies)
                    {
                        mySqlAccess.DeleteStudentChoices(id);
                    }
                }
            }
            else
            {
                MessageBox.Show("No abandoned student choices found!",
                                    "Database Integrity",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
            }
        }

        private void CourseSelector01_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked == true)
            {
                int nYear = Convert.ToInt32(trackDateTxtBx.Text);
                int nModule = Convert.ToInt32(trackTxtBx.Text);

                if (((RadioButton)sender).Text == "UN")
                {
                    FillAssignedCoursesView(nYear, nModule, -1);
                    asignedCoursesView.ContextMenuStrip = assignStudentMenu;
                    courseSelectorCombo.Items.Clear();
                    foreach (RadioButton rb in AssignedCoursesSelector)
                    {
                        courseSelectorCombo.Items.Add(rb.Text);
                    }
                }
                else
                {
                    FillAssignedCoursesView(nYear, nModule, mySqlAccess.CourseDict[((RadioButton)sender).Text]);
                    asignedCoursesView.ContextMenuStrip = null;
                    courseSelectorCombo.Items.Clear();
                }
                asignedCoursesView.Tag = ((RadioButton)sender).Text;
            }
        }

        private void RefreshBtn_Click(object sender, EventArgs e)
        {
            CancelEventArgs cancelEventArgs = new CancelEventArgs(false);
            TrackDateTxtBx_Validating(trackDateTxtBx, cancelEventArgs);
            if (false == cancelEventArgs.Cancel)
            {
                TrackDateTxtBx_Validated(trackDateTxtBx, new EventArgs());
                TrackTxtBx_Validating(trackTxtBx, cancelEventArgs);
            }

            if (false == cancelEventArgs.Cancel)
            {
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

                foreach (KeyValuePair<string, int> kvp in mySqlAccess.CourseDict)
                {
                    if (true == mySqlAccess.TestCourseidInAssignments(nYear, nTrack, kvp.Value))
                    {
                        AssignedCoursesSelector[i].Text = kvp.Key;
                        AssignedCoursesSelector[i].Visible = true;
                        AssignedCoursesSelector[i].Enabled = true;
                        i += 1;
                    }
                }

                // check for unassigned students
                if (true == mySqlAccess.TestCourseidInAssignments(nYear, nTrack, -1))
                {
                    AssignedCoursesSelector[i].Text = "UN";
                    AssignedCoursesSelector[i].Visible = true;
                    AssignedCoursesSelector[i].Enabled = true;
                }

                AssignedCoursesSelector[0].Checked = true;
                FillAssignedCoursesView(nYear, nTrack, mySqlAccess.CourseDict[AssignedCoursesSelector[0].Text]);

                int nStudentId = Convert.ToInt32(asignedCoursesView.Rows[1].Cells[1].Value);
                if (true == mySqlAccess.TestAssignmentIsLocked(nYear, nTrack, mySqlAccess.CourseDict[AssignedCoursesSelector[0].Text], nStudentId))
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
                AsignedCoursesView_CellContentClick(sender, new DataGridViewCellEventArgs(0, 0));
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

                    mySqlAccess.UpdateStudentAssignment(
                        mySqlAccess.CourseDict[courseSelectorCombo.SelectedItem.ToString()],
                        Convert.ToInt32(dgvSelectedRow.Cells[0].Value));

                    if (mySqlAccess.TestCourseidInAssignments(nYear, nModule, -1) == false)
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
                        FillAssignedCoursesView(nYear, nModule, -1);
                    }
                }
            }
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

        private void StudentTableView_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            studentTableView.Rows[e.RowIndex].Selected = true;
            CurrentStudentTableIndex = e.RowIndex;
            modifyStudentBtn.Enabled = true;
            deleteStudentBtn.Enabled = true;
            newStudentBtn.Enabled = true;
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

                studentDetailView.Visible = false;
                studentDetailView.ClearSelection();
                studentDetailView.DataSource = null;
                studentDetailView.Refresh();

                studentDetailView.DataSource = mySqlAccess.GetAllCoursesForStudent(
                    Convert.ToInt32(asignedCoursesView.Rows[e.RowIndex].Cells[1].Value),
                    Convert.ToInt32(trackDateTxtBx.Text));
                studentDetailView.DataMember = "course_assignments";
                studentDetailView.Refresh();

                studentDetailView.Columns[0].HeaderText = "Track";
                studentDetailView.Columns[1].HeaderText = "Course";

                studentDetailView.Visible = true;
            }
        }

        private void CourseDetailsSelector_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked == true)
            {
                int nCourseID = Convert.ToInt32(((RadioButton)sender).Tag);
                courseCountTxtBx.Text = FillCourseSelectionView(nCourseID).ToString();
            }
        }

        private void LimoUiTabControl_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPageIndex == 1)
            {
                ModifyStudentBtn_Click(sender, e);
            }

            if (e.TabPageIndex == 3)
            {
                int nCourseID = Convert.ToInt32(defaultCourseSelectorRB.Tag);
                courseCountTxtBx.Text = FillCourseSelectionView(nCourseID).ToString();
                defaultCourseSelectorRB.Checked = true;
            }
        }


        // input control validation
        private void StudentName_Validating(object sender, CancelEventArgs e)
        {
            if (((TextBox)sender).Text.Length < 3)
            {
                UserInputErrorProvider.SetIconAlignment((TextBox)sender, ErrorIconAlignment.MiddleLeft);
                UserInputErrorProvider.SetIconPadding((TextBox)sender, 2);
                UserInputErrorProvider.SetError((TextBox)sender, "Field can't be empty or shorter than 3 characters");

                e.Cancel = true;
                ((TextBox)sender).Select(0, ((TextBox)sender).Text.Length);
                if (Convert.ToInt32(((TextBox)sender).Tag) == 0)
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
            UserInputErrorProvider.SetError((TextBox)sender, "");
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

        private void SchooltypeTxtBx_Validating(object sender, CancelEventArgs e)
        {
            if (mySqlAccess.SchoolTypesDict.ContainsKey(schooltypeTxtBx.Text) == false)
            {
                UserInputErrorProvider.SetIconAlignment((TextBox)sender, ErrorIconAlignment.MiddleLeft);
                UserInputErrorProvider.SetIconPadding((TextBox)sender, 2);
                UserInputErrorProvider.SetError((TextBox)sender, "Enter a valid school type");

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

        private void SchooltypeTxtBx_Validated(object sender, EventArgs e)
        {
            schooltypeTxtBx.ForeColor = SystemColors.WindowText;
            UserInputErrorProvider.SetError((TextBox)sender, "");
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

        private void GradeClassTxtBx_Validating(object sender, CancelEventArgs e)
        {
            if (mySqlAccess.ClassesDict.ContainsKey(gradeClassTxtBx.Text) == false)
            {
                UserInputErrorProvider.SetIconAlignment((TextBox)sender, ErrorIconAlignment.MiddleLeft);
                UserInputErrorProvider.SetIconPadding((TextBox)sender, 2);
                UserInputErrorProvider.SetError((TextBox)sender, "Enter valid values for <grade><class>");
                e.Cancel = true;
                ((TextBox)sender).Select(0, ((TextBox)sender).Text.Length);
                if (Convert.ToInt32(((TextBox)sender).Tag) == 0)
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
            UserInputErrorProvider.SetError((TextBox)sender, "");
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

        private void TrackDateTxtBx_Validating(object sender, CancelEventArgs e)
        {

        }

        private void TrackDateTxtBx_Validated(object sender, EventArgs e)
        {
            ((TextBox)sender).ForeColor = SystemColors.WindowText;
            UserInputErrorProvider.SetError((TextBox)sender, "");
        }

        private void TrackTxtBx_Validating(object sender, CancelEventArgs e)
        {

        }


        private void Form1_Shown(object sender, EventArgs e)
        {
            if (mySqlAccess.LimoInputConnection == null)
            {
                toolStripStatusLabel1.Text = "Environment Variable limoinput_connection must be defined.";
            }
            else
            {
                if (true == mySqlAccess.InitializeDbConnection())
                {
                    FillStudentTableView();

                    studentTableView.RowEnter += new DataGridViewCellEventHandler(StudentTableView_RowEnter);
                    studentTableView.Rows[0].Selected = true;
                    countCoursesTxtBx.Text = "0";

                    mySqlAccess.GetAvailableCoursesClassesAndSchooltypes();

                    label17.Text = mySqlAccess.Prefixes[0];
                    label18.Text = mySqlAccess.Prefixes[1];
                    label19.Text = mySqlAccess.Prefixes[2];
                    label20.Text = mySqlAccess.Prefixes[3];
                    label21.Text = mySqlAccess.Prefixes[4];

                    // create table array with checkboxes to view/select student course choices
                    StudentChoicesCheckBoxes = new CheckBox[mySqlAccess.TotalCourses];
                    int[] location_X = { 3, 75, 147, 219, 291 };
                    int[] location_Y = { 23, 57, 91, 125, 159, 193, 227, 261, 295, 329 };
                    int courseId = 0;

                    for (int column = 0; column < mySqlAccess.CourseGroups; column++)
                    {
                        for (int row = 0; row < mySqlAccess.Laengen[column]; row++)
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
                }
                else
                {
                    toolStripStatusLabel1.Text = "Environment Variable limoinput_connection has wrong format.";
                }
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
            Dictionary<int, int> listRemoveId = new Dictionary<int, int>();
            List<int> listAddId = new List<int>();

            if (isUpdateMode == true)
            {
                // update existing choice
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

                mySqlAccess.UpdateStudentChoices(listRemoveId, listAddId, StudentChoicesCheckBoxes.Count(), nStudentId);
            }
            else
            {
                // insert a new choice
                foreach (CheckBox abox in StudentChoicesCheckBoxes)
                {
                    if (abox.Checked == true)
                    {
                        int course_id = Convert.ToInt32(((string[])abox.Tag)[0]);
                        listAddId.Add(course_id);
                    }
                }
                mySqlAccess.InsertStudentChoices(listAddId, nStudentId);
            }

            ClearStudentChoices();
            GetStudentChoices(nStudentId);
        }

        private void GetStudentChoices(int nStudentId)
        {
            DataSet dsChoices = mySqlAccess.GetStudentChoices(nStudentId);
            foreach (DataRow arow in dsChoices.Tables[0].Rows)
            {
                StudentChoicesCheckBoxes[(int)arow[2] - 1].Checked = true;
                ((string[])StudentChoicesCheckBoxes[(int)arow[2] - 1].Tag)[1] = ((int)arow[0]).ToString();
            }
        }

        private void RefreshStudentTableView(int nSelectedStudent)
        {
            studentTableView.ClearSelection();
            studentTableView.DataSource = null;
            studentTableView.DataMember = "";
            studentTableView.Refresh();

            studentTableView.DataSource = mySqlAccess.GetStudentTable();
            studentTableView.DataMember = "students";
            studentTableView.Refresh();

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

        private void FillStudentTableView()
        {
            studentTableView.DataSource = mySqlAccess.GetStudentTable();
            studentTableView.DataMember = "students";

            studentTableView.Columns[0].HeaderText = "ID";
            studentTableView.Columns[1].HeaderText = "Name";
            studentTableView.Columns[2].HeaderText = "Nachname";
            studentTableView.Columns[3].HeaderText = "Schultyp";
            studentTableView.Columns[4].HeaderText = "Stufe";
            studentTableView.Columns[5].HeaderText = "Klasse";
        }

        private int FillAssignedCoursesView(int nYear, int nTrack, int nCourseId)
        {
            int nRowCount = 0;
            asignedCoursesView.Visible = false;
            asignedCoursesView.ClearSelection();
            asignedCoursesView.DataSource = null;
            asignedCoursesView.Refresh();

            asignedCoursesView.DataSource = mySqlAccess.GetTrackFromAssignments(nYear, nTrack, nCourseId);
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
            nRowCount = ((DataSet)asignedCoursesView.DataSource).Tables[0].Rows.Count;

            countStudentsTxtBx.Text = nRowCount.ToString();
            return nRowCount;
        }

        private void GetCurrentStudent(int rowIndex)
        {
            toolStripStatusLabel1.Text = "";
            textBox1.Text = studentTableView.Rows[rowIndex].Cells[0].Value.ToString();
            textBox2.Text = studentTableView.Rows[rowIndex].Cells[1].Value.ToString();
            textBox3.Text = studentTableView.Rows[rowIndex].Cells[2].Value.ToString();
            schooltypeTxtBx.Text = studentTableView.Rows[rowIndex].Cells[3].Value.ToString();
            gradeClassTxtBx.Text = studentTableView.Rows[rowIndex].Cells[4].Value.ToString() + 
                studentTableView.Rows[rowIndex].Cells[5].Value.ToString();
        }

        private int FillCourseSelectionView(int nCourseID)
        {
            courseSelectionView.Visible = false;
            courseSelectionView.ClearSelection();
            courseSelectionView.DataSource = null;
            courseSelectionView.Refresh();

            courseSelectionView.DataSource = mySqlAccess.GetCourse(nCourseID);
            courseSelectionView.DataMember = "student_choices";
            courseSelectionView.Refresh();

            courseSelectionView.Columns[0].HeaderText = "ID";
            courseSelectionView.Columns[1].HeaderText = "Name";
            courseSelectionView.Columns[2].HeaderText = "Nachname";

            courseSelectionView.Columns[0].Visible = false;

            courseSelectionView.Sort(courseSelectionView.Columns[2], ListSortDirection.Ascending);
            courseSelectionView.Visible = true;

            return ((DataSet)courseSelectionView.DataSource).Tables[0].Rows.Count;
        }

    }
}
