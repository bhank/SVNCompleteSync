using System;
using System.Collections.Generic;
using NDesk.Options;

namespace SvnClient
{
    public enum Command
    {
        UnknownDefaultThingy,
        CompleteSync,
        CheckoutUpdate,
    }

    public class Parameters
    {
        public Command Command { get; private set; } // completesync, checkoutupdate
        public string Message { get; private set; } // commit message
        public bool UpdateBeforeCompleteSync { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Path { get; private set; }
        public string Url { get; private set; }
        public bool Revert { get; private set; }
        public bool Cleanup { get; private set; }
        public bool DeleteUnversioned { get; private set; }
        public bool TrustServerCert { get; private set; }
        public bool Mkdir { get; private set; }

        public static bool TryParse(IList<string> args, out Parameters parameters)
        {
            parameters = null;

            var p = new Parameters
                        {
                            Message = "Committed by SvnClient",
                        };

            var optionSet = new OptionSet
                        {
                            {"username=", "SVN username", v => p.Username = v},
                            {"password=", "SVN password", v => p.Password = v},
                            {"k|trust-server-cert", "Trust unsigned/expired server certificates",v => p.TrustServerCert = (v != null)},
                            {"m|message=", "Commit message for CompleteSync commit or CheckoutUpdate remote directory add", v => p.Message = v},
                            {"u|updatebeforesync", "SVN update first (CompleteSync only)", v => p.UpdateBeforeCompleteSync = (v != null)},
                            {"revert", "Revert changes first (CheckoutUpdate only)", v => p.Revert = (v != null)},
                            {"cleanup", "CleanUp working copy first (CheckoutUpdate only)",v => p.Cleanup = (v != null)},
                            {"deleteunversioned", "Delete unversioned files from working copy first (CheckoutUpdate only)",v => p.DeleteUnversioned = (v != null)},
                            {"cleanworkingcopy", "Same as --revert --cleanup --deleteunversioned (CheckoutUpdate only)", v => p.Revert = p.Cleanup = p.DeleteUnversioned = (v != null)},
                            {"mkdir", "Create the URL if it doesn't exist (CheckoutUpdate only)", v => p.Mkdir = (v != null)},
                        };
            var extraArgs = optionSet.Parse(args);
            
            if(extraArgs.Count == 0)
            {
                Console.Error.WriteLine("No command specified.");
                ShowUsage(optionSet);
                return false;
            }

            Command command;
            if(EnumTryParse(extraArgs[0], true, out command))
            {
                p.Command = command;
            }
            else
            {
                Console.Error.WriteLine("Unknown command: " + extraArgs[0]);
                ShowUsage(optionSet);
                return false;
            }

            switch(p.Command)
            {
                case Command.CompleteSync:
                    if(extraArgs.Count < 2)
                    {
                        Console.Error.WriteLine("path is required");
                        ShowUsage(optionSet);
                        return false;
                    }
                    p.Path = extraArgs[1];
                    break;
                case Command.CheckoutUpdate:
                    if(extraArgs.Count < 3)
                    {
                        Console.Error.WriteLine("URL and path are required");
                        ShowUsage(optionSet);
                        return false;
                    }
                    p.Url = extraArgs[1];
                    p.Path = extraArgs[2];
                    break;
            }

            parameters = p;
            return true;
        }

        private static void ShowUsage(OptionSet optionSet)
        {
            var commands = new List<string>(Enum.GetNames(typeof (Command)));
            commands.RemoveAt(0);

            var exeName = AppDomain.CurrentDomain.FriendlyName;
            Console.Error.WriteLine(string.Format("{0} {{ {1} }}", exeName, string.Join(" | ", commands.ToArray())));
            Console.Error.WriteLine(string.Format("{0} {1} PATH [options]", exeName, Command.CompleteSync));
            Console.Error.WriteLine(string.Format("{0} {1} URL PATH [options]", exeName, Command.CheckoutUpdate));
            optionSet.WriteOptionDescriptions(Console.Error);
        }

        private static bool EnumTryParse<TEnum>(string value, bool ignoreCase, out TEnum result)
        {
            if(Enum.IsDefined(typeof(TEnum), value))
            {
                result = (TEnum) Enum.Parse(typeof (TEnum), value);
                return true;
            }

            if(ignoreCase)
            {
                foreach(var enumValue in Enum.GetNames(typeof(TEnum)))
                {
                    if(string.Equals(enumValue, value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result = (TEnum) Enum.Parse(typeof (TEnum), enumValue);
                        return true;
                    }
                }
            }

            result = default(TEnum);
            return false;
        }
    }
}
