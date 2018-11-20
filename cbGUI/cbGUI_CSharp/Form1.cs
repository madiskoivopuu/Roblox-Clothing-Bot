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
using System.Net.Http.Headers;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using System.Diagnostics;
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
        int g_iAssetType; // Download type for a certain asset 12-Shirts, 13-TShirts, 14-Pants
        string g_sAssetType; // String can contain Pants, Shirts or T-Shirts
        int g_iItemCount; // Amount of assets downloaded
        List<String> g_MainConfig = new List<String>();


        //Delegates (required for cross-thread operation)

        public delegate void AddToListDelegate(String txt);
        public delegate void ClearListDelegate();
        public delegate void ChangePictureBoxDelegate(String path);
        public delegate void VoidDelegate();


        public Form1()
        {
            InitializeComponent();
            CreateNecessaryDirectoriesIfNeeded();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e) // Download button
        {
            StartDownload();
        }

        private void StartDownload()
        {
            if(g_iAssetType == 0)
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

        private void ChangePicture(String path)
        {
            pictureBox2.Image = new Bitmap(path);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) // Download thread
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
                    String urlPost1 = "https://search.roblox.com/catalog/json?Category=3&SortType=2&Subcategory=" + g_iAssetType + "&AggregationFrequency=3&PageNumber=" + i + "&CatalogContext=1"; // Downloads clothes based on bestselling->past week
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
                                    this.Invoke(new ChangePictureBoxDelegate(ChangePicture), filepath);
                                    client2.Dispose();
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

        private void button2_Click(object sender, EventArgs e) // Upload button
        {
            StartUpload();
        }

        private void StartUpload()
        {
            if(!UploadWorker.IsBusy)
            {
                LoadConfig();
                listBox1.Items.Clear();
                listBox1.Items.Add("BOT | Starting a upload session.");
                DisableAllButtons();

                UploadWorker.RunWorkerAsync();
            }
        }

        private void LoadConfig()
        {
            String text = File.ReadAllText(Environment.CurrentDirectory + "\\Settings\\Config.cfg");
            dynamic dynJson = JsonConvert.DeserializeObject(text);

            foreach(var setting in dynJson)
            {
                foreach(var value in setting)
                {
                    g_MainConfig.Add(value.ToString());
                }
            }
        }

        private void UploadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            HttpClient client = new HttpClient();

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", g_MainConfig[0]),
                new KeyValuePair<string, string>("password", g_MainConfig[1])
            });

            var response = client.PostAsync("https://api.roblox.com/v2/login", requestContent).Result;
            String resultContent = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                String loginCookie = ReadLoginCookie(response);
                for (int asset = 12; asset <= 14; ++asset)
                {
                    if (UploadWorker.CancellationPending) // Stops loop when a cancellation has been requested (Stop button has been pressed)
                    {
                        break;
                    }
                    CheckChangeAssetType(asset);
                    for (int i = 5; i < g_MainConfig.Count; ++i)
                    {
                        if (UploadWorker.CancellationPending) // Stops loop when a cancellation has been requested (Stop button has been pressed)
                        {
                            break;
                        }
                        int clothing = 0;
                        this.Invoke(new ClearListDelegate(ClearList));
                        this.Invoke(new AddToListDelegate(AddToList), "BOT | Uploading assets to group " + g_MainConfig[i]);

                        String mainDir = Environment.CurrentDirectory + "\\Files\\" + g_sAssetType;
                        DirectoryInfo d = new DirectoryInfo(mainDir);
                        FileInfo[] Files = d.GetFiles("*.png");
                        foreach (FileInfo file in Files)
                        {
                            if (UploadWorker.CancellationPending) // Stops loop when a cancellation has been requested (Stop button has been pressed)
                            {
                                break;
                            }
                            ++clothing;
                            if(clothing > 250) // Stops loop when it reaches over 250 uploaded assets
                            {
                                break;
                            }

                            response = client.GetAsync("https://www.roblox.com/build/upload").Result;
                            resultContent = response.Content.ReadAsStringAsync().Result;
                            String rvt = GetRequestVerificationToken(resultContent);

                            String name = file.ToString();
                            name = name.Substring(0, name.Length - 4);
                            name = DecodeFrom64(name);
                            MessageBox.Show(name);

                            String fileLoc = "./Files/" + g_sAssetType + "/" + file;

                            int min, max;
                            Int32.TryParse(g_MainConfig[3], out min);
                            Int32.TryParse(g_MainConfig[4], out max);
                            int price = GetRandomInt(min, max);

                            var content = new MultipartFormDataContent();
                            var values = new[]
                            {
                                new KeyValuePair<string, string>("__RequestVerificationToken", rvt),
                                new KeyValuePair<string, string>("assetTypeId", g_iAssetType.ToString()),
                                new KeyValuePair<string, string>("groupId", g_MainConfig[i]),
                                new KeyValuePair<string, string>("onVerificationPage", "False"),
                                new KeyValuePair<string, string>("isOggUploadEnabled", "True"),
                                new KeyValuePair<string, string>("isTgaUploadEnabled", "True"),
                                new KeyValuePair<string, string>("name", name),
                            };

                            foreach (var keyValuePair in values)
                            {
                                content.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                            }

                            var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(fileLoc));
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = file.ToString()
                            };
                            content.Add(fileContent);

                            var result = client.PostAsync("https://www.roblox.com/build/upload", content).Result;
                            resultContent = response.Content.ReadAsStringAsync().Result;

                            /*List<String> argv = new List<String>();
                            argv.Add("no_use");
                            argv.Add(name.ToString());
                            argv.Add(g_iAssetType.ToString());
                            argv.Add(fileLoc);
                            argv.Add(g_MainConfig[i]);
                            argv.Add(g_MainConfig[2]);
                            argv.Add(price.ToString());
                            argv.Add(g_MainConfig[0]);
                            argv.Add(g_MainConfig[1]);

                            var engine = Python.CreateEngine();
                            var searchPaths = engine.GetSearchPaths();
                            searchPaths.Add(@"C:\Python27\Lib");
                            searchPaths.Add(@"C:\Python27\Lib\site-packages");
                            engine.SetSearchPaths(searchPaths);
                            engine.GetSysModule().SetVariable("argv", argv);

                            var script = engine.CreateScriptSourceFromFile("upload.py");
                            var scope = engine.CreateScope();
                            script.Execute(scope);

                            engine.Runtime.Shutdown();*/

                            if (!UploadWorker.CancellationPending) // Stops loop when a cancellation has been requested (Stop button has been pressed)
                            {
                                this.Invoke(new AddToListDelegate(AddToList), "BOT | UPLOAD SUCCESS - Group " + g_MainConfig[i] + " - Item " + clothing + " - " + name);
                            }
                        }
                    }
                }
            }
            else
            {
                this.Invoke(new AddToListDelegate(AddToList), "BOT | Login failed. Recheck your login details. If they are correct then it may be a login captcha.");
                client.Dispose();
            }

            NormalizeAssetTypes();
            this.Invoke(new VoidDelegate(EnableAllButtons));
        }

        private void DisableAllButtons()
        {
            button1.Enabled = false;
            button2.Enabled = false;

            // The Stop button
            button3.Enabled = true;
        }
        private void EnableAllButtons()
        {
            button1.Enabled = true;
            button2.Enabled = true;

            // The Stop button
            button3.Enabled = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) //Shirts
        {
            if(checkBox1.Checked)
            {
                g_iAssetType = 12;
                checkBox2.Enabled = false;
                checkBox3.Enabled = false;
                g_sAssetType = "Shirts";
            }
            else
            {
                g_iAssetType = 0;
                checkBox2.Enabled = true;
                checkBox3.Enabled = true;
                g_sAssetType = "";
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e) //T-Shirts
        {
            if (checkBox2.Checked)
            {
                g_iAssetType = 13;
                checkBox1.Enabled = false;
                checkBox3.Enabled = false;
                g_sAssetType = "T-Shirts";
            }
            else
            {
                g_iAssetType = 0;
                checkBox1.Enabled = true;
                checkBox3.Enabled = true;
                g_sAssetType = "";
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e) //Pants
        {
            if (checkBox3.Checked)
            {
                g_iAssetType = 14;
                checkBox1.Enabled = false;
                checkBox2.Enabled = false;
                g_sAssetType = "Pants";
            }
            else
            {
                g_iAssetType = 0;
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;
                g_sAssetType = "";
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e) // Start Page textbox
        {
            if (textBox1.Text != "")
            {
                Int32.TryParse(textBox1.Text, out g_iStartPage);
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
                Int32.TryParse(textBox2.Text, out g_iStopPage);
            }
            else
            {
                g_iStopPage = 1;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        // Stocks by me

        public string GetRequestVerificationToken(String find)
        {
            String val = "name=__RequestVerificationToken";
            String readInto = "";
            int startRead = find.IndexOf(val, 0) + val.Length + 20;

            MessageBox.Show(startRead.ToString());

            while(find[startRead] != '>')
            {
                readInto += find[startRead];
                ++startRead;
            }

            return readInto;
        }

        public void CreateNecessaryDirectoriesIfNeeded()
        {
            String check = Environment.CurrentDirectory + "\\Files";
            if (!Directory.Exists(check))
            {
                Directory.CreateDirectory(check);
                Directory.CreateDirectory(check + "\\Shirts");
                Directory.CreateDirectory(check + "\\T-Shirts");
                Directory.CreateDirectory(check + "\\Pants");
            }
            check = Environment.CurrentDirectory + "\\Settings";
            if (!Directory.Exists(check))
            {
                Directory.CreateDirectory(check);
                File.Create(check + "\\Config.cfg");
            }
        }

        public int GetRandomInt(int min, int max)
        {
            Random rnd = new Random();
            int random = rnd.Next(min, max);

            return random;
        }

        public void NormalizeAssetTypes()
        {
            g_iAssetType = 0;
            g_sAssetType = "";
        }

        public int CheckChangeAssetType(int sth)
        {
            if (sth == 12)
            {
                g_sAssetType = "Shirts";
                g_iAssetType = 12;
            }
            if (sth == 13)
            {
                g_sAssetType = "T-Shirts";
                g_iAssetType = 2;
            }
            if (sth == 14)
            {
                g_sAssetType = "Pants";
                g_iAssetType = 12;
            }
            return 0;
        }

        // Stocks by other people

        static public String ReadLoginCookie(HttpResponseMessage response)
        {
            var pageUri = response.RequestMessage.RequestUri;

            var cookieContainer = new CookieContainer();
            var cookie = "";
            IEnumerable<string> cookies;
            if (response.Headers.TryGetValues("set-cookie", out cookies))
            {
                foreach (var c in cookies)
                {
                    if(c.Contains(".ROBLOSECURITY="))
                    {
                        int start = 15;
                        while(c[start] != ';')
                        {
                            cookie += c[start];
                            ++start;
                        }
                    }
                }
            }
            // I just had to do the worst shit to get ONE SINGLE COOKIE
            // Couldn't find a better way and since I didn't want to waste much time, I just had to use this
            return cookie;
        }

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

        private void button3_Click(object sender, EventArgs e) // Stop Button
        {
            UploadWorker.CancelAsync();
            listBox1.Items.Clear();
            listBox1.Items.Add("BOT | Uploading cancelled.");
            EnableAllButtons();
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