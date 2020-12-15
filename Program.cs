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
        private const string _outputFolderPath = "c:\\outdir\\";
        private const string _outputTextFilename = "output.txt";
        private const string _outputTextFilepath = _outputFolderPath + _outputTextFilename;
        private const string _textCommandSuffix = " fileID = fopen('" + _outputTextFilepath + "', 'w'); fprintf(fileID, '%s', answerString); fclose(fileID); exit; \"";
        private const string _imageFilename = "outputImage.png";
        private const string _imageFullpath = _outputFolderPath + _imageFilename;
        private const string _imageCommandSuffix = " whereToStoreImage=fullfile('" + _outputFolderPath + "',['" + _imageFilename + "']); saveas(";
        private const string _imageCommandPostfix = ", whereToStoreImage); exit; \"";

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
            string customOutputVar = null;
            
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";

            switch (messages[0])
            {
                case "!help":
                {
                    EmbedBuilder builder = new EmbedBuilder();

                    builder.Title = "List of commands";
                    builder.Description = "Equations and sequences of characters in all other input elements must not contain spaces.\nInput elements in [] are mandatory. Input elements in () are optional.\nAdditional variables may represent any additional data.";
                    builder.Color = Color.Blue;
                    builder.Footer = new EmbedFooterBuilder().WithText("This bot uses MATLAB for calculations");

                    builder.AddField("!help", "Provides the list of available commands.");
                    builder.AddField("!solve", "Solves the given equation.\nSyntax: `!solve [equation] [unknown var] (additional vars)`\nExample: `!solve a*x+3+b==18 x a,b`");
                    builder.AddField("!solveSystem", "Solves the given system of equations.\nSyntax: `!solveSystem [equations] [unknown vars] (additional vars)`\nExample: `!solveSystem x+y==a,x-y==b x,y a,b`");
                    builder.AddField("!diff", "Takes derivative of the given function.\nSyntax: `!diff [equation] [dependent var] (additional vars) [diff number]`\nExample: `!diff x^a x a 1`");
                    builder.AddField("!limit", "Takes limit of the given function.\nSyntax: `!limit [function] [dependent var] (additional vars) [point]`\nExample 1: `!limit x^2 x 5`\nExample 2: `!limit a*symsum(1/2^i,i,1,x) x a,i Inf`");
                    builder.AddField("!integrate", "Makes an indefinite integration of the given function.\nSyntax: `!integrate [function] [slice along var] (additional vars)`\nExample: `!integrate exp(x)*b+3*a-4 x a,b`");
                    builder.AddField("!integrateDefinite", "Makes an definite integration of the given function.\nSyntax: `!integrateDefinite [function] [slice along var] (additional vars) [bottom limit] [upper limit]`\nExample: `!integrateDefinite 2*x+3 x 1 4`");
                    builder.AddField("!graphic", "Creates a graphic of the given function.\nSyntax: `!graphic [function] [argument var] [argument left limit] [argument right limit]`\nExample: `!graphic x^2 x -5 5`");
                    if (msg.Author.Id == 284956691391578112) builder.AddField("!custom", "Runs custom matlab code.\nSyntax: !custom [output type] [output variable] [code]\nAvailable output types: text, image\nExample 1: `!custom text answer answer = 3 + 5;`\nExample 2: `!custom image gcf x = -5:0.01:5; y = x.^2; gcf = figure('visible','off'); plot(x,y);`");

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
                    if (messages[2].Contains(','))
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
                case "!limit":
                {
                    if (messages.Length < 4 || messages.Length > 5)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }
                    if (messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 4)
                    {
                        codeToExecute += "syms " + messages[2] + "; f = @(" + messages[2] + ") " + messages[1] + "; answer = limit(f(" + messages[2] + "), " + messages[2] + ", " + messages[3] + "); answerString = evalc('answer'); ";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2] + " " + messages[3].Replace(',', ' ') + "; f = @(" + messages[2] + "," + messages[3] + ") " + messages[1] + "; answer = limit(f(" + messages[2] + "," + messages[3] + "), " + messages[2] + ", " + messages[4] + "); answerString = evalc('answer'); ";
                    }

                    break;
                }
                case "!diff":
                {
                    if (messages.Length < 4 || messages.Length > 5)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }
                    if (messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2] + "; f = @(" + messages[2] + ") " + messages[1] + "; answer = diff(f(" + messages[2] + "), " + messages[2] + ", " + messages[3] + "); answerString = evalc('answer'); ";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2] + " " + messages[3] + "; f = @(" + messages[2] + ", " + messages[3] + ") " + messages[1] + "; answer = diff(f(" + messages[2] + "," + messages[3] + "), " + messages[2] + ", " + messages[4] + "); answerString = evalc('answer'); ";
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
                    if (messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    prefix = $"f({messages[2]}) = ";
                    postfix = " + C";

                    if (messages.Length == 3)
                    {
                        codeToExecute += "syms " + messages[2] +$"; f = @({messages[2]}) {messages[1]}; answer = int(f({messages[2]}),'" + messages[2] + "'); answerString = evalc('answer');";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2][0] + " " + messages[3].Replace(',', ' ') + $"; f = @({messages[2]},{messages[3]}) {messages[1]}; answer = int(f({messages[2]}, {messages[3]}),'" + messages[2] + $"'); answerString = evalc('answer');";
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
                    if (messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    if (!messages[1].Contains("==")) messages[1] = messages[1].Replace("=", "==");

                    if (messages.Length == 5)
                    {
                        codeToExecute += "syms " + messages[2] + $"; f = @({messages[2]}) {messages[1]}; answer = int(f({messages[2]}),'" + messages[2] + $"', {messages[3]}, {messages[4]}); answerString = evalc('answer');";
                    }
                    else
                    {
                        codeToExecute += "syms " + messages[2][0] + " " + messages[3].Replace(',', ' ') + $"; f = @({messages[2]},{messages[3]}) {messages[1]}; answer = int(f({messages[2]}, {messages[3]}),'" + messages[2] + $"', {messages[3]}, {messages[4]}); answerString = evalc('answer');";
                    }

                    break;
                }
                case "!graphic":
                {
                    if (messages.Length != 5)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");
                    }
                    if (messages[2].Contains(','))
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }

                    messages[1] = messages[1].Replace("^", ".^");

                    outputsText = false;

                    codeToExecute += "syms y " + messages[2] + "; " + messages[2] + " = [" + messages[3] + ":0.01:" + messages[4] + "]; y = " + messages[1] + "; answerImage = figure('visible','off'); plot(" + messages[2] + ",y); ";

                    break;
                }
                case "!custom":
                {
                    if (msg.Author.Id != 284956691391578112)
                    {
                        await msg.Channel.SendMessageAsync("You cannot use this command.");

                        return;
                    }
                    if (messages.Length < 4)
                    {
                        await msg.Channel.SendMessageAsync("Wrong input.");

                        return;
                    }
                    messages[1] = messages[1].ToLower();
                    if (messages[1] != "text" && messages[1] != "image")
                    {
                        await msg.Channel.SendMessageAsync("Wrong input. Output must be text or image.");

                        return;
                    }

                    customOutputVar = messages[2];

                    string customCode = "";
                    for (int i = 3; i < messages.Length; i++)
                    {
                        customCode += messages[i];
                        if (i < messages.Length - 1) customCode += " ";
                    }

                    if (messages[1] == "text")
                    {
                        codeToExecute = customCode + $" answerString = evalc('{messages[2]}'); ";
                    }
                    else
                    {
                        outputsText = false;

                        codeToExecute = customCode;
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

            try
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "Output";
                builder.WithColor(Color.Blue);

                if (outputsText)
                {
                    startInfo.Arguments = _pathToMatlab + codeToExecute + _textCommandSuffix;
                    process.StartInfo = startInfo;

                    process.Start();

                    await Task.Delay(1000);

                    while (!process.HasExited)
                    {
                        await Task.Delay(100);
                    }

                    string answer = File.ReadAllText(_outputTextFilepath);
                    answer = answer.Replace("*", "\\*");
                    answer = answer.Replace("answer =\n", "");
                    answer = answer.Replace("answerString =\n", "");
                    answer = answer.Replace("ans =\n", "");
                    answer = answer.Replace("answer =", "");
                    answer = answer.Replace("answerString =", "");
                    answer = answer.Replace("ans =", "");
                    if (customOutputVar != null)
                    {
                        answer = answer.Replace("customOutputVar =\n", "");
                        answer = answer.Replace("customOutputVar =", "");
                    }
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
                    if (customOutputVar != null) startInfo.Arguments = _pathToMatlab + codeToExecute + _imageCommandSuffix + customOutputVar + _imageCommandPostfix;
                    else startInfo.Arguments = _pathToMatlab + codeToExecute + _imageCommandSuffix + "answerImage" + _imageCommandPostfix;
                    process.StartInfo = startInfo;

                    process.Start();

                    await Task.Delay(1000);

                    while (!process.HasExited)
                    {
                        await Task.Delay(100);
                    }

                    builder.ImageUrl = $"attachment://{_imageFilename}";

                    if (process.ExitCode == 0)
                    {
                        await msg.Channel.SendFileAsync(_imageFullpath, embed: builder.Build());
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("An error occured. Check your input.");
                    }
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
    }
}
