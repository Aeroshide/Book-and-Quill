using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Lyrics_Service
{
    public partial class Form1 : Form
    {
        private LyricsRequester lyricsRequester;
        private string songCache;
        private int spotifyProcessIndex;
        private Process spotifyProcess;

        public Form1()
        {
            InitializeComponent();
            string fileContents = ReadFileContext("secret.aeroshide", "Book-and-Quill");
            lyricsRequester = new LyricsRequester(fileContents);
        }

        public string ReadFileContext(string fileName, string appName)
        {
            // Get the APPDATA environment variable
            string appData = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrEmpty(appData))
            {
                // Handle error: APPDATA environment variable not found.
                throw new InvalidOperationException("APPDATA environment variable not found.");
            }

            // Construct the file path
            string path = Path.Combine(appData, "Aeroshide", appName, fileName);

            // Check if the file exists before attempting to open
            if (!File.Exists(path))
            {
                // Handle error: file could not be opened.
                throw new FileNotFoundException("The specified file could not be found.", path);
            }

            // Read and return the file content
            return File.ReadAllText(path);
        }

        public (SpotifyProcessStatus suc, string ret) HookSpotify()
        {
            Process[] processCandidate = Process.GetProcessesByName("Spotify");
            

            if (spotifyProcess == null)
            {
                for (int i = 0; i < processCandidate.Length; i++)
                {
                    Debug.WriteLine("Searching for spotify");
                    if (processCandidate[i].MainWindowTitle != "")
                    {
                        Debug.WriteLine("Spotify process found!");
                        spotifyProcessIndex = i;
                    }

                }

            }

            try
            {
                spotifyProcess = processCandidate[spotifyProcessIndex];
            }
            catch (Exception ex)
            {
                spotifyProcess = null;
                Debug.WriteLine("Spotify is not running.");
                return (SpotifyProcessStatus.Closed, "Not detected");
            }
            


            if (spotifyProcess != null)
            {

                string windowTitle = spotifyProcess.MainWindowTitle;
                Debug.WriteLine($"Spotify window title: {windowTitle}");
                if (windowTitle == null || windowTitle == ""|| windowTitle == " ")
                {
                    return (SpotifyProcessStatus.Background, "Minimized");
                }
                else if (windowTitle == "Spotify Free")
                {
                    return (SpotifyProcessStatus.Idle, "Not Playing");
                }
                return (SpotifyProcessStatus.Running, windowTitle);

            }

            return (SpotifyProcessStatus.Closed, "Not detected");

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;
            flowLayoutPanel1.AutoScroll = true;


        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        public string LyricsProcessor(string lyrics)
        {
            // Add a double newline before [Chorus] or [Verse] with numbers
            var processedLyrics = System.Text.RegularExpressions.Regex.Replace(lyrics, @"\s*\[(Chorus|Verse)\s*\d+\]", "\n\n$0");
            processedLyrics = System.Text.RegularExpressions.Regex.Replace(processedLyrics, @"\[[^\]]+\]", "");

            // Add a newline before lowercase followed by uppercase (e.g., "bB")
            processedLyrics = System.Text.RegularExpressions.Regex.Replace(processedLyrics, @"([a-z])([A-Z])", "$1\n$2");

            // Add a newline before an uppercase letter that is not preceded by a space, a single quote, a double quote, or an opening parenthesis
            processedLyrics = System.Text.RegularExpressions.Regex.Replace(processedLyrics, @"(?<![ '(\)])(?<![a-z])([A-Z])", "\n$1");


            // Remove any extra spaces resulting from the previous replacements
            processedLyrics = processedLyrics.Trim();

            return processedLyrics;
        }






        private async void button1_Click(object sender, EventArgs e)
        {


        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            var spotifyProcess = HookSpotify();

            switch (spotifyProcess.suc)
            {
                case SpotifyProcessStatus.Running:
                    pictureBox1.BackColor = Color.LimeGreen;
                    label2.Text = "Hooked to Spotify";

                    if (spotifyProcess.ret != songCache)
                    {
                        Debug.WriteLine("hook:" + spotifyProcess.ret);
                        Debug.WriteLine("cache:" + songCache);
                        try
                        {
                            var lyrics = await Task.Run(() => lyricsRequester.GetLyricsAsync(spotifyProcess.ret));
                            label1.Text = LyricsProcessor(lyrics);
                            songCache = spotifyProcess.ret;
                        }
                        catch (Exception ex)
                        {
                            label1.Text = LyricsProcessor("Lyrics not available for this song!");
                        }
                    }
                    else
                        return;

                    break;
                case SpotifyProcessStatus.Idle:
                    pictureBox1.BackColor = Color.Yellow;
                    label2.Text = "Spotify is not playing audio";
                    break;
                case SpotifyProcessStatus.Background:
                    pictureBox1.BackColor = Color.Orange;
                    label2.Text = "Spotify is minimized";
                    break;
                case SpotifyProcessStatus.Closed:
                    pictureBox1.BackColor = Color.Red;
                    label2.Text = "Spotify not detected";
                    break;
            }

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }

    public enum SpotifyProcessStatus
    {
        Running,
        Idle,
        Background,
        Closed
    }
}
