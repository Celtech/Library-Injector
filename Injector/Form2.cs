using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Injector
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        int i = 0;

        private void Form2_Load(object sender, EventArgs e)
        {

            listView1.Sorting = SortOrder.Ascending;
            Process[] processlist = Process.GetProcesses();

            foreach(Process theprocess in processlist)
            {
                try
                {
                    imageList1.Images.Add(Icon.ExtractAssociatedIcon(theprocess.MainModule.FileName));
                    listView1.Items.Add(theprocess.ProcessName).ImageIndex = i;
                    listView1.Items[i].SubItems.Add(theprocess.Id.ToString());
                    i++;
                }
                catch 
                {
                    imageList1.Images.Add(Properties.Resources.inject);
                    listView1.Items.Add(theprocess.ProcessName).ImageIndex = i;
                    listView1.Items[i].SubItems.Add(theprocess.Id.ToString());
                    i++;
                };
            }
        }

        public string curt = "";

        public string getTextBoxValue()
        {
            return curt;
        }

        public void CloseForm()
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            curt = listView1.SelectedItems[0].Text + ".exe";
            Form2 f1 = (Form2)Application.OpenForms["Form2"];
            f1.CloseForm();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }
    }
}
