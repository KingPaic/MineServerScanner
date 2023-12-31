using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace WindowsFormsApp1
{



    public partial class MainForm : Form
    {
        MinecraftServerScanner serverScanner = new MinecraftServerScanner();
        FindIps fip = new FindIps();

        // Get the current directory where the script is located
        string scriptDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Define the file path and file content
        string filePath;

        public MainForm()
        {
            InitializeComponent();
            filePath = Path.Combine(scriptDirectory, "lastread.txt");

            //textBox1.Text= "54.39.158.149,51.178.176.74,51.91.62.101,146.59.252.149";

            try
            {
                // Read the contents of the file
                textBox1.Text = File.ReadAllText(filePath);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            //            textBox1.Text = "";
            numericUpDown1.Value = 40000;
            numericUpDown2.Value = 42000;
            numericUpDown3.Value = 300;

            listView1.MouseDoubleClick += ListView1_MouseDoubleClick;
            // Handle the ColumnClick event to enable sorting.
            listView1.ColumnClick += ListView_ColumnClick;

            SetTransparencyForControls(this.Controls);
            button4.Visible = false;
            this.Opacity = 1;





        }

        private void CreateGroupsByIP()
        {
            Dictionary<string, ListViewGroup> ipGroups = new Dictionary<string, ListViewGroup>();

            foreach (ListViewItem item in listView1.Items)
            {
                string ipAddress = item.SubItems[0].Text;

                if (!ipGroups.ContainsKey(ipAddress))
                {
                    // Create a new group for the IP address
                    ListViewGroup group = new ListViewGroup(ipAddress, HorizontalAlignment.Left);
                    listView1.Groups.Add(group);
                    ipGroups[ipAddress] = group;
                }

                // Assign the item to its corresponding group
                item.Group = ipGroups[ipAddress];
            }
        }


        private void CreateGroupsByVersion()
        {
            Dictionary<string, ListViewGroup> ipGroups = new Dictionary<string, ListViewGroup>();

            foreach (ListViewItem item in listView1.Items)
            {
                string ipAddress = item.SubItems[2].Text;

                if (!ipGroups.ContainsKey(ipAddress))
                {
                    // Create a new group for the IP address
                    ListViewGroup group = new ListViewGroup(ipAddress, HorizontalAlignment.Left);
                    listView1.Groups.Add(group);
                    ipGroups[ipAddress] = group;
                }

                // Assign the item to its corresponding group
                item.Group = ipGroups[ipAddress];
            }
        }





        // Function to set transparency for controls recursively
        private void SetTransparencyForControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Label label)
                {
                    // Set the background color of the label to be transparent
                    label.BackColor = System.Drawing.Color.Transparent;
                }

            }
        }



        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Create an instance of the ListViewItemComparer class
            ListViewItemComparer sorter = new ListViewItemComparer(e.Column);

            // Sort the ListView using the comparer
            listView1.ListViewItemSorter = sorter;
            listView1.Sort();
        }


        // Custom ListViewItemComparer class for sorting
        private class ListViewItemComparer : System.Collections.IComparer
        {
            private int column;

            public ListViewItemComparer(int column)
            {
                this.column = column;
            }

            public int Compare(object x, object y)
            {
                return string.Compare(((ListViewItem)x).SubItems[column].Text, ((ListViewItem)y).SubItems[column].Text);
            }
        }


        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Check if an item is selected
            if (listView1.SelectedItems.Count > 0)
            {
                // Get the selected item
                ListViewItem selectedItem = listView1.SelectedItems[0];

                string selectedText;
                if (selectedItem.SubItems[1].Text != "")
                {
                    // Get the text of the selected item
                     selectedText = selectedItem.Text + ":" + selectedItem.SubItems[1].Text;
                }
                else {
                     selectedText = selectedItem.Text;
                }

                // Copy the selected text to the clipboard
                Clipboard.SetText(selectedText);

                // Optionally, show a message to indicate that the text has been copied
                MessageBox.Show("Text copied to clipboard: " + selectedText, "Copy Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private async void button1_Click(object sender, EventArgs e)
        {

         
            if (serverScanner.running || fip.running)
            {
                return;
            }
            listView1.Items.Clear();
            listView1.Groups.Clear();

            try
            {
                string fileContent = textBox1.Text;
                // Write the file content to the specified file path
                File.WriteAllText(filePath, fileContent);
                Console.WriteLine("File saved successfully.");
            }
            catch
            {

            }


            string[] ipAddresses = textBox1.Text.Split(','); // Replace with your desired IP addresses
            int startPort = (int)numericUpDown1.Value;
            int endPort = (int)numericUpDown2.Value; // Adjust the port range as needed


            Dictionary<string, string> serverVersions2 = await serverScanner.ScanServersAsync(ipAddresses, startPort, endPort, progressBar1, listView1, (int)numericUpDown3.Value);

            
            CreateGroupsByVersion();


        }


        private async void button3_Click(object sender, EventArgs e)
        {

            if (serverScanner.running || fip.running)
            {
                return;
            }

            listView1.Items.Clear();
            listView1.Groups.Clear();

           await fip.ScanIPRangeAsync(textBox1.Text, progressBar1, listView1,500, (int)numericUpDown3.Value);

            button4.Visible = listView1.Items.Count > 0;
           // CreateGroupsByIP();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportListViewToCsv(listView1, saveFileDialog.FileName);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }






        // Function to export ListView items to CSV
        private void ExportListViewToCsv(ListView listView, string filePath)
        {
            try
            {
                // Create or overwrite the CSV file
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write the column headers to the CSV
                    writer.WriteLine(string.Join(";", listView.Columns.Cast<ColumnHeader>().Select(column => column.Text)));

                    // Iterate through each ListViewItem and write its sub-items to the CSV
                    foreach (ListViewItem item in listView.Items)
                    {
                        var values = item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(subItem => subItem.Text);
                        writer.WriteLine(string.Join(";", values));
                    }
                }

                MessageBox.Show("ListView data has been exported to CSV successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            foreach (ListViewItem item in listView1.Items)
            {
                textBox1.Text = textBox1.Text+ item.SubItems[0].Text+",";
            }
            textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 1);
            }
    }
}