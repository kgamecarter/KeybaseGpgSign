using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeybaseGpgSign
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        private void BtnRun_Click(object sender, EventArgs e)
        {
            btnRun.Enabled = false;
            var lines = tbCmd.Lines;
            var sb = new StringBuilder();
            sb.Append(lines[2].Substring(3));
            sb.Remove(5, 1);
            sb.Length -= 5;
            sb.Append(" |");
            sb.Append(lines[3].Substring(2).Replace("'", ""));
            sb.Length -= 4;
            var psi = new ProcessStartInfo("cmd")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var p = Process.Start(psi);
            var input = p.StandardInput;
            input.WriteLine(sb.ToString());
            input.WriteLine("exit");
            string line;
            sb.Clear();
            while ((line = p.StandardOutput.ReadLine()) != null)
            {
                if (line == "-----END PGP MESSAGE-----")
                {
                    sb.Append("-----END PGP MESSAGE-----");
                    p.StandardOutput.Close();
                    break;
                }
                else if (sb.Length != 0)
                {
                    sb.AppendLine(line);
                }
                else if (line == "-----BEGIN PGP MESSAGE-----")
                    sb.AppendLine("-----BEGIN PGP MESSAGE-----");
            }
            var result = sb.ToString();

            var formDataDictionary = new Dictionary<string, string>()
            {
                ["sig"] = result,
            };
            string url = null;
            for (int i = 4; i < lines.Length; i++)
            {
                if (lines[i].Contains("--data-urlencode"))
                {
                    line = lines[i].Substring(19, lines[i].Length - 22);
                    var data = line.Split(new string[] { "=\"" }, StringSplitOptions.None);
                    formDataDictionary.Add(data[0], data[1]);
                }
                else if (lines[i].Contains("https://keybase.io/"))
                {
                    url = lines[i].Trim();
                }
            }
            var formData = new FormUrlEncodedContent(formDataDictionary);

            using (HttpClient client = new HttpClient())
            {
                var t = client.PostAsync(url, formData);
                t.Wait();
                var t2 = t.Result.Content.ReadAsStringAsync();
                t2.Wait();
                result = t2.Result;
            }
            MessageBox.Show(result);
            tbCmd.Focus();
            tbCmd.SelectAll();
            btnRun.Enabled = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            tbCmd.Focus();
        }
    }
}
