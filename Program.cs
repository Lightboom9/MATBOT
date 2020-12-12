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
            bool systemOutput = false;
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
                    builder.Description = "Equations and sequences of characters in all other input elements must not contain spaces.\nInput elements in [] are mandatory. Input elements in () are optional.\nAdditional variables may represent any additional data.";
                    builder.Color = Color.Blue;
                    builder.Footer = new EmbedFooterBuilder().WithText("This bot uses MATLAB");

                    builder.AddField("!help", "Provides the list of available commands.");
                    builder.AddField("!solve", "Solves the given equation.\nSyntax: `!solve [equation] [unknown var] (additional vars)`\nExample: `!solve a*x+3+b==18 x a,b`");
                    builder.AddField("!solveSystem", "Solves the given system of equations.\nSyntax: `!solveSystem [equations] [unknown vars] (additional vars)`\nExample: `!solveSystem x+y==a,x-y==b x,y a,b`");
                    builder.AddField("!integrate", "Integrates the given function.\nSyntax: `!integrate [function] [slice along var] (additional vars)`\nExample: `!integrate exp(x)*b+3*a-4 x a,b`");
                    builder.AddField("!integrateDefinite", "Makes a definite integration of the given function.\nSyntax: `!integrateDefinite [function] [slice along var] (additional vars) [bottom limit] [upper limit]`\nExample: `!integrateDefinite 2*x+3 x 1 4`");

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
                    if (messages[1].Contains(',') || messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2] + "; answer = solve(" + messages[1] + "," + messages[2] + "); answerString = evalc('answer');";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2].Replace(',', ' ') + " " + messages[3].Replace(',', ' ') + "; answer = solve(" + messages[1] + "," + messages[2] + "); answerString = evalc('answer');";
                    }

                    break;
                }
                case "!solveSystem":
                {
                    if (messages.Length < 3 || messages.Length > 4)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    systemOutput = true;

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2].Replace(',', ' ') + "; answer = solve([" + messages[1] + "],[" + messages[2] + "]); answerString = ''; ";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2].Replace(',', ' ') + " " + messages[3].Replace(',', ' ') + "; answer = solve([" + messages[1] + "],[" + messages[2] + "]); answerString = ''; ";
                    }

                    string[] syms = messages[2].Split(',');

                    for (int i = 0; i < syms.Length; i++)
                    {
                        codeToExecute += "str" + i + " = evalc('answer." + syms[i] + "'); answerString = strcat(answerString, str" + i + "); ";
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
                    if (messages[1].Contains(',') || messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    prefix = $"f({messages[2]}) = ";
                    postfix = " + C";

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2] +$"; f = inline('{messages[1]}', '{messages[2]}'); answer = int(f({messages[2]}),'" + messages[2] + "'); answerString = evalc('answer');";
                    }
                    else
                    {
                        string parms = "";
                        for (int i = 0; i < messages[3].Length; i += 2)
                        {
                            parms += "'" + messages[3][i] + "'";
                            if (i + 1 != messages[3].Length) parms += ",";
                        }

                        codeToExecute += "syms " + messages[2][0] + " " + messages[3].Replace(',', ' ') + $"; f = inline('{messages[1]}', '{messages[2]}', {parms}); answer = int(f({messages[2]}, {messages[3]}),'" + messages[2] + $"'); answerString = evalc('answer');";
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
                    if (messages[1].Contains(',') || messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 5)
                    {
                        codeToExecute += "syms " + messages[2] + $"; f = inline('{messages[1]}', '{messages[2]}'); answer = int(f({messages[2]}),'" + messages[2] + $"', {messages[3]}, {messages[4]}); answerString = evalc('answer');";
                    }
                    else
                    {
                        string parms = "";
                        for (int i = 0; i < messages[3].Length; i += 2)
                        {
                            parms += "'" + messages[3][i] + "'";
                            if (i + 1 != messages[3].Length) parms += ",";
                        }

                        codeToExecute += "syms " + messages[2][0] + " " + messages[3].Replace(',', ' ') + $"; f = inline('{messages[1]}', '{messages[2]}', {parms}); answer = int(f({messages[2]}, {messages[3]}),'" + messages[2] + $"', {messages[3]}, {messages[4]}); answerString = evalc('answer');";
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
                    answer = answer.Replace("answer =\n", "");
                    answer = answer.Replace("answerString =\n", "");
                    answer = answer.Replace("ans =\n", "");
                    answer = answer.Replace("answer =", "");
                    answer = answer.Replace("answerString =", "");
                    answer = answer.Replace("ans =", "");
                    answer = answer.Replace("Empty sym: 0-by-1", "Answer cannot be found or there's an error in the input");
                    answer = answer.Trim();
                    answer = prefix + answer + postfix;

                    if (systemOutput)
                    {
                        string[] syms = messages[2].Split(',');
                        string[] lines = answer.Split('\n');

                        string newAnswer = "";

                        for (int i = 0, j = 0, k = 0; i < lines.Length; i++)
                        {
                            if (String.IsNullOrWhiteSpace(lines[i]))
                            {
                                j = 0;
                                k++;

                                newAnswer += "\n";

                                continue;
                            }

                            newAnswer += syms[k] + GetSubscriptNumChar(j) + ": " + lines[i] + "\n";
                            j++;
                        }

                        answer = newAnswer;
                    }

                    builder.WithDescription(answer);

                    if (process.ExitCode == 0)
                    {
                        await msg.Channel.SendMessageAsync("", embed: builder.Build());
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("An error occured. Check your input.");
                    }
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

        private char GetSubscriptNumChar(int num)
        {
            switch (num)
            {
                case 0:
                {
                    return '\u2080';
                }
                case 1:
                {
                    return '\u2081';
                }
                case 2:
                {
                    return '\u2082';
                }
                case 3:
                {
                    return '\u2083';
                }
                case 4:
                {
                    return '\u2084';
                }
                case 5:
                {
                    return '\u2085';
                }
                case 6:
                {
                    return '\u2086';
                }
                case 7:
                {
                    return '\u2087';
                }
                case 8:
                {
                    return '\u2088';
                }
                case 9:
                {
                    return '\u2089';
                }
                default:
                {
                    Console.WriteLine("An error occured in GetSubscriptNumChar method. Argument: " + num);

                    return ' ';
                }
            }
        }

        private bool IsFileInUse(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
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
