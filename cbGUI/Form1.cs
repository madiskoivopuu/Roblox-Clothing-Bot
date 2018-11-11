using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

/*
   _____                  _    _          _____      _   
  / ____|                | |  | |        / ____|    | |  
 | (___  _ __   __ _ _ __| | _| |_   _  | |     __ _| |_ 
  \___ \| '_ \ / _` | '__| |/ / | | | | | |    / _` | __|
  ____) | |_) | (_| | |  |   <| | |_| | | |___| (_| | |_ 
 |_____/| .__/ \__,_|_|  |_|\_\_|\__, |  \_____\__,_|\__|
        | |                       __/ |                  
        |_|                      |___/                   
*/

namespace cbGUI_CSharp
{
    public partial class Form1 : Form
    {
        //Global Variables
        int g_iStartPage = 0;
        int g_iStopPage = 1;
        int g_iDownloadType; // Download type for a certain asset 12-Shirts, 13-TShirts, 14-Pants
        string g_sAssetType; // String can contain Pants, Shirts or T-Shirts
        int g_iItemCount; // Amount of assets downloaded

        //Delegates (required for cross-thread operation)

        public delegate void AddToListDelegate(String txt);
        public delegate void ClearListDelegate();


        public Form1()
        {
            InitializeComponent();
            CreateNecessaryDirectoriesIfNeeded();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartDownload();
        }

        private void StartDownload()
        {
            if(g_iDownloadType == 0)
            {
                listBox1.Items.Clear();
                listBox1.Items.Add("BOT | Cannot download without any asset type selected.");
                return;
            }

            if (!backgroundWorker1.IsBusy)
            {
                listBox1.Items.Clear();
                listBox1.Items.Add("BOT | Download starting.");
                DisableAllButtons();

                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void AddToList(String txt)
        {
            listBox1.Items.Add(txt);
        }

        private void ClearList()
        {
            listBox1.Items.Clear();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //HTTP stuff
            for (int i = g_iStartPage; i <= g_iStopPage;)
            {
                this.BeginInvoke(new ClearListDelegate(ClearList));
                String msgString = "BOT | Downloading assets from catalog page " + i;
                this.BeginInvoke(new AddToListDelegate(AddToList), msgString);
                ++i;
                HttpClient client = new HttpClient();
                try
                {
                    String urlPost1 = "https://search.roblox.com/catalog/json?Category=3&SortType=2&Subcategory=" + g_iDownloadType + "&AggregationFrequency=3&PageNumber=" + i + "&CatalogContext=1"; // Downloads clothes based on bestselling->past week
                    HttpResponseMessage response = client.GetAsync(urlPost1).Result;
                    response.EnsureSuccessStatusCode();
                    HttpContent content = response.Content;

                    String sContent = content.ReadAsStringAsync().Result;
                    dynamic dynJson = JsonConvert.DeserializeObject(sContent);
                    foreach (var item in dynJson)
                    {
                        ++g_iItemCount;
                        String urlPost2 = "https://assetgame.roblox.com/asset/?id=" + item.AssetId;
                        try
                        {
                            response = client.GetAsync(urlPost2).Result;
                            content = response.Content;
                            sContent = content.ReadAsStringAsync().Result;

                            int pFrom = sContent.IndexOf("<url>") + "<url>".Length;
                            int pTo = sContent.LastIndexOf("</url>");

                            if (pTo - pFrom < 0)
                            {
                                String errString = "BOT | FAILURE - Item " + g_iItemCount + " - " + item.AssetId + " - " + item.Name;
                                this.Invoke(new AddToListDelegate(AddToList), errString);
                            }
                            else
                            {
                                String result = sContent.Substring(pFrom, pTo - pFrom);

                                try
                                {
                                    String urlPost3 = result;

                                    String assetName = item.Name;
                                    String encodedName = EncodeTo64(assetName);
                                    String filepath = Environment.CurrentDirectory + "\\Files\\" + g_sAssetType + "\\" + encodedName + ".png";

                                    WebClient client2 = new WebClient();
                                    client2.DownloadFile(result, filepath);

                                    String success = "BOT | SUCCESS - Item " + g_iItemCount + " - " + item.AssetId + " - " + item.Name;
                                    this.Invoke(new AddToListDelegate(AddToList), success);
                                }
                                catch (HttpRequestException)
                                {
                                    String errString = "BOT | FAILURE - " + g_iItemCount + " - " + item.AssetId + " - " + item.Name;
                                    this.Invoke(new AddToListDelegate(AddToList), errString);
                                }
                            }
                        }
                        catch (HttpRequestException exception)
                        {
                            this.Invoke(new AddToListDelegate(AddToList), "BOT | Failed to download (stage 2).");
                            String errString = "BOT ERROR: " + exception.Message;
                            this.Invoke(new AddToListDelegate(AddToList), errString);
                            return;
                        }
                    }
                }
                catch (HttpRequestException exception)
                {
                    this.Invoke(new AddToListDelegate(AddToList), "BOT | Failed to download (stage 1).");
                    String errString = "BOT ERROR: " + exception.Message;
                    this.Invoke(new AddToListDelegate(AddToList), errString);
                    client.Dispose();
                    return;
                }
                client.Dispose();
               
            }
        }

        private void backgroundWorker1_RunWorkerComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Invoke(new ClearListDelegate(ClearList));
            this.Invoke(new AddToListDelegate(AddToList),  "BOT | Successfully downloaded " + g_iItemCount + " assets.");
            EnableAllButtons();
            g_iItemCount = 0;
        }

        private void DisableAllButtons()
        {
            button1.Enabled = false;
            button2.Enabled = false;
        }
        private void EnableAllButtons()
        {
            button1.Enabled = true;
            button2.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) //Shirts
        {
            if(checkBox1.Checked)
            {
                g_iDownloadType = 12;
                checkBox2.Enabled = false;
                checkBox3.Enabled = false;
                g_sAssetType = "Shirts";
            }
            else
            {
                g_iDownloadType = 0;
                checkBox2.Enabled = true;
                checkBox3.Enabled = true;
                g_sAssetType = "";
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e) //T-Shirts
        {
            if (checkBox2.Checked)
            {
                g_iDownloadType = 13;
                checkBox1.Enabled = false;
                checkBox3.Enabled = false;
                g_sAssetType = "T-Shirts";
            }
            else
            {
                g_iDownloadType = 0;
                checkBox1.Enabled = true;
                checkBox3.Enabled = true;
                g_sAssetType = "";
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e) //Pants
        {
            if (checkBox3.Checked)
            {
                g_iDownloadType = 14;
                checkBox1.Enabled = false;
                checkBox2.Enabled = false;
                g_sAssetType = "Pants";
            }
            else
            {
                g_iDownloadType = 0;
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;
                g_sAssetType = "";
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e) // Start Page textbox
        {
            if (textBox1.Text != "")
            {
                g_iStartPage = Convert.ToInt32(textBox1.Text);
            }
            else
            {
                g_iStartPage = 1;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e) // Stop Page textbox
        {
            if(textBox2.Text != "")
            {
                g_iStopPage = Convert.ToInt32(textBox2.Text);
            }
            else
            {
                g_iStopPage = 1;
            }
        }

        // Stocks by me

        private void CreateNecessaryDirectoriesIfNeeded()
        {
            String check = Environment.CurrentDirectory + "\\Files";
            if(!Directory.Exists(check))
            {
                Directory.CreateDirectory(check);
                Directory.CreateDirectory(check + "\\Shirts");
                Directory.CreateDirectory(check + "\\T-Shirts");
                Directory.CreateDirectory(check + "\\Pants");
            }
        }

        // Stocks by other people

        static public string EncodeTo64(string toEncode)
        {

            byte[] toEncodeAsBytes

                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);

            string returnValue

                  = System.Convert.ToBase64String(toEncodeAsBytes);

            returnValue = returnValue.Replace("/", "-");

            return returnValue;

        }

        static public string DecodeFrom64(string encodedData)
        {

            encodedData = encodedData.Replace("-", "/");

            byte[] encodedDataAsBytes

                = System.Convert.FromBase64String(encodedData);

            string returnValue =

               System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

            return returnValue;

        }

        /*public static string Replace(string original, char replacement, params char[] replaceables)
        {
            StringBuilder builder = new StringBuilder(original.Length);
            HashSet<char> replaceable = new HashSet<char>(replaceables);
            foreach (Char character in original)
            {
                if (replaceable.Contains(character))
                    builder.Append(replacement);
                else
                    builder.Append(character);
            }
            return builder.ToString();
        }*/
    }
}