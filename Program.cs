using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MATBOT
{
    class Program
    {
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private DiscordSocketClient _client;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.MessageReceived += MessageLogger;

            string token = File.ReadAllText(@"c:\outdir\botToken.txt");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private const string _pathToMatlab = "/C e:\\Programs\\MATLAB\\bin\\matlab.exe -batch \"";
        private const string _outputTextFilename = "c:\\outdir\\output.txt";
        private const string _endOfStuff = " fileID = fopen('" + _outputTextFilename + "', 'w'); fprintf(fileID, '%s', answerString); fclose(fileID); exit; \"";
        private const string _imageFilename = "outputImage.png";

        public async Task MessageLogger(SocketMessage msg)
        {
            if (String.IsNullOrEmpty(msg.Content) || msg.Author.IsBot) return;
            if (msg.Content[0] != '!') return;

            string fullMessage = msg.Content.Replace('\n', ' ');
            
            string[] messages = fullMessage.Split(' ');
            string codeToExecute = "";

            bool outputsText = true;
            string prefix = "";
            string postfix = "";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";

            switch (messages[0])
            {
                case "!embed":
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    
                    builder.Title = "This is a title.";
                    builder.AddField("This is a field.", "And *this* is a value.", true);
                    builder.ImageUrl = $"attachment://{_imageFilename}";

                    builder.WithColor(Color.Blue);
                    await msg.Channel.SendFileAsync(@"c:\outdir\" + _imageFilename, embed: builder.Build());

                    return;
                }
                case "!help":
                {
                    EmbedBuilder builder = new EmbedBuilder();

                    builder.Title = "List of commands";
                    builder.Description = "Equations and sequences of elements in all other input elements must not contain spaces.\nInput elements in [] are mandatory. Input elements in () are optional.\nAdditional variables may represent any additional data.";
                    builder.Color = Color.Blue;
                    builder.Footer = new EmbedFooterBuilder().WithText("This bot uses MATLAB");

                    builder.AddField("!help", "Provides the list of available commands.");
                    builder.AddField("!solve", "Solves the given equation.\nSyntax: `!solve [equation] [unknown var] (additional vars)`\nExample: `!solve a*x+3+b==18 x a,b`");
                    builder.AddField("!integrate", "Integrates the given equation.\nSyntax: `!integrate [function] [slice along var] (additional vars)`\nExample: `!integrate exp(x)*b+3*a-4 x a,b`");
                    builder.AddField("!integrateDefinite", "Makes a definite integration of the given equation.\nSyntax: `!integrateDefinite [function] [slice along var] (additional vars) [bottom limit] [upper limit]`\nExample: `!integrateDefinite 2*x+3 x 1 4`");

                    await msg.Channel.SendMessageAsync("", embed: builder.Build());

                    return;
                }
                case "!solve":
                {
                    if (messages.Length < 3 || messages.Length > 4)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2].Replace(',', ' ') + "; answerToThisDarkQuestion = solve(" + messages[1] + "," + messages[2] + "); answerString = evalc('answerToThisDarkQuestion');";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2].Replace(',', ' ') + " " + messages[3].Replace(',', ' ') + "; answerToThisDarkQuestion = solve(" + messages[1] + "," + messages[2] + "); answerString = evalc('answerToThisDarkQuestion');";
                    }

                    break;
                }
                case "!integrate":
                {
                    if (messages.Length < 3 || messages.Length > 4)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }
                    if (messages[2].Length > 1)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    messages[1] = messages[1].Replace("=", "==");

                    prefix = $"f({messages[2]}) = ";
                    postfix = " + C";

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2] +$"; f = inline('{messages[1]}', '{messages[2]}'); answerToThisDarkQuestion = int(f({messages[2]}),'" + messages[2] + "'); answerString = evalc('answerToThisDarkQuestion');";
                    }
                    else
                    {
                        string parms = "";
                        for (int i = 0; i < messages[3].Length; i += 2)
                        {
                            parms += "'" + messages[3][i] + "'";
                            if (i + 1 != messages[3].Length) parms += ",";
                        }

                        codeToExecute += "syms " + messages[2][0] + " " + messages[3].Replace(',', ' ') + $"; f = inline('{messages[1]}', '{messages[2]}', {parms}); answerToThisDarkQuestion = int(f({messages[2]}, {messages[3]}),'" + messages[2] + $"'); answerString = evalc('answerToThisDarkQuestion');";
                    }

                    break;
                }
                case "!integrateDefinite":
                {
                    if (messages.Length < 5 || messages.Length > 6)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }
                    if (messages[2].Length > 1)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 5)
                    {
                        codeToExecute += "syms " + messages[2] + $"; f = inline('{messages[1]}', '{messages[2]}'); answerToThisDarkQuestion = int(f({messages[2]}),'" + messages[2] + $"', {messages[3]}, {messages[4]}); answerString = evalc('answerToThisDarkQuestion');";
                    }
                    else
                    {
                        string parms = "";
                        for (int i = 0; i < messages[3].Length; i += 2)
                        {
                            parms += "'" + messages[3][i] + "'";
                            if (i + 1 != messages[3].Length) parms += ",";
                        }

                        codeToExecute += "syms " + messages[2][0] + " " + messages[3].Replace(',', ' ') + $"; f = inline('{messages[1]}', '{messages[2]}', {parms}); answerToThisDarkQuestion = int(f({messages[2]}, {messages[3]}),'" + messages[2] + $"', {messages[3]}, {messages[4]}); answerString = evalc('answerToThisDarkQuestion');";
                    }

                    break;
                }
                default:
                {
                    await msg.Channel.SendMessageAsync("Wrong input.");

                    return;
                }
            }

            await msg.Channel.SendMessageAsync("Processing...");

            startInfo.Arguments = _pathToMatlab + codeToExecute + _endOfStuff;
            process.StartInfo = startInfo;
            try
            {
                process.Start();

                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Output";
                builder.WithColor(Color.Blue);

                if (outputsText)
                {
                    await Task.Delay(5000);

                    FileInfo info = new FileInfo(_outputTextFilename);
                    while (IsFileInUse(info))
                    {
                        await Task.Delay(1000);
                    }

                    await Task.Delay(1000);

                    string answer = File.ReadAllText(_outputTextFilename);
                    answer = answer.Replace("*", "\\*");
                    answer = answer.Replace("answerToThisDarkQuestion =", "");
                    answer.Replace("Empty sym: 0-by-1", "Answer can't be found or there's an error in the input");
                    answer = answer.Trim();
                    answer = prefix + answer + postfix;

                    builder.WithDescription(answer);

                    await msg.Channel.SendMessageAsync("", embed: builder.Build());
                }
                else
                {

                }

                process.Kill();
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occured.\n" + e.Message);
                await msg.Channel.SendMessageAsync("An error occured.");
            }
        }

        protected bool IsFileInUse(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }
            return false;
        }
    }
}
