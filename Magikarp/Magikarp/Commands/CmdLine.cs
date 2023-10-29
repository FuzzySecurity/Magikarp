using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Commands
{
    internal class CmdLine
    {
        // Types
        //============================
        
        /// <summary>
        /// Enum of the different types of values that can be parsed
        /// </summary>
        public enum ValueType
        {
            None,
            String,
            Int32,
            Int64,
            UInt32,
            UInt64,
            Boolean,
            Enum
        }

        /// <summary>
        /// Struct which defines the input parameters for the argument
        /// </summary>
        public class ArgOption
        {
            public String Name = String.Empty;
            public String LongName = String.Empty;
            public String ShortName = String.Empty;
            public String HelpText = String.Empty;
            public Boolean Required = false;
            public ValueType ArgumentType = ValueType.None;
            public Type EnumValidator = null;
        }
        
        // Helper methods
        //============================

        // Simple wrapper to get a full string from args
        private static String GetStringFromArgs(String[] sArgs)
        {
            return String.Join(" ", sArgs);
        }
        
        /// <summary>
        /// Print auto-generated help text for the list of provided arguments
        /// </summary>
        /// <param name="lArguments">Takes a list of ArgOption's</param>
        /// <param name="sPreFlag">The command string delimiter, e.g. / or --</param>
        /// <param name="sPostFlag">The delimiter specifying the end of the command, e.g. :</param>
        /// <returns>void</returns>
        public static void PrintHelp(List<ArgOption> lArguments, String sPreFlag = "/", String sPostFlag = ":")
        {
            // We want to have aligned flags and help text
            // |_ we need to find the longest combination of LongName and ShortName
            // |_ then we can pad the help text to align it
            Int32 iLongestFlag = 0;
            foreach (ArgOption oArg in lArguments)
            {
                Int32 iLength = oArg.LongName.Length + oArg.ShortName.Length + 2;
                if (iLength > iLongestFlag)
                {
                    iLongestFlag = iLength;
                }
            }
            
            // We need to increase the length by the length of the pre and post flags
            // |_ we add 5 for extra padding and ( ) around ShortName if applicable
            iLongestFlag += sPreFlag.Length + sPostFlag.Length + 5;
            
            // Now we can print the help text
            foreach (ArgOption oArg in lArguments)
            {
                // If this is a boolean flag, we print space instead of the post flag
                String sFinalPostFlag = sPostFlag;
                if (oArg.ArgumentType == ValueType.Boolean)
                {
                    sFinalPostFlag = " ";
                }
                
                // What combination of long and short name do we have?
                String sFlagText = String.Empty;
                if (!String.IsNullOrEmpty(oArg.LongName) && !String.IsNullOrEmpty(oArg.ShortName))
                {
                    sFlagText = String.Format("{0}{2}{1} ({0}{3}{1})", sPreFlag, sFinalPostFlag, oArg.LongName, oArg.ShortName);
                } else if (!String.IsNullOrEmpty(oArg.LongName))
                {
                    sFlagText = String.Format("{0}{2}{1}", sPreFlag, sFinalPostFlag, oArg.LongName);
                } else if (!String.IsNullOrEmpty(oArg.ShortName))
                {
                    sFlagText = String.Format("{0}{2}{1}", sPreFlag, sFinalPostFlag, oArg.ShortName);
                }
                
                String sHelpText = String.Format("{0}{1}", sFlagText.PadRight(iLongestFlag), oArg.HelpText);
                Console.WriteLine(sHelpText);
            }
        }

        // Regex
        //============================
        
        // Based on a full string, get the value of an arbitrary argument
        // |_ 1st regex: find the argument and match everything after it
        // |_ 2nd regex: extract the value from the match
        private static String GetArgByRegex(String sFullArg, String sLongName, String sShortName, Boolean bIsBooleanFlag = false)
        {
            // Placeholder for regex expression
            String sRegex = String.Empty;
            
            // Do we have a short and long name?
            if (!String.IsNullOrEmpty(sLongName) && !String.IsNullOrEmpty(sShortName))
            {
                sRegex = @"(?i)(-|--|/)(" + sLongName + "|" + sShortName + @")(:|\s+?)(.+)";
            }
            // Do we have a long name?
            else if (!String.IsNullOrEmpty(sLongName))
            {
                sRegex = @"(?i)(-|--|/)(" + sLongName + @")(:|\s+?)(.+)";
            }
            else
            {
                sRegex = @"(?i)(-|--|/)(" + sShortName + @")(:|\s+?)(.+)";
            }
            
            // Does the regex match?
            if (Regex.IsMatch(sFullArg, sRegex))
            {
                // Get the match
                Match m = Regex.Match(sFullArg, sRegex);
                
                // We have a match, now we need to get the value from that match
                String sValue = m.Groups[4].Value;
                
                // Build new regex to get the value
                sRegex = "(?i)(|\"|\\s+?)(-\\w|-\\w.+?|\\w|\\w.+?)(\"|)($|\\s(-|--|\\/))";
                
                // Does the regex match?
                if (Regex.IsMatch(sValue, sRegex))
                {
                    // Get the match
                    m = Regex.Match(sValue, sRegex);
                    
                    // Get the value
                    return m.Groups[2].Value;
                }
                else
                {
                    // No match on the value
                    return String.Empty;
                }
            }
            else
            {
                // We special case boolean flags
                // |_ flag passed without value -> true
                if (bIsBooleanFlag)
                {
                    sRegex = sRegex.Substring(0, sRegex.Length - 12) + @"(:|\s+?|$)";
                    if (Regex.IsMatch(sFullArg, sRegex))
                    {
                        return "true";
                    }
                }
                
                // No match
                return String.Empty;
            }
        }
        
        // Parser methods
        //============================

        /// <summary>
        /// Read and validate the user defined arguments
        /// </summary>
        /// <param name="lArguments">Takes a list of ArgOption's</param>
        /// <returns>List<ArgOption></returns>
        private static List<ArgOption> ValidateArguments(List<ArgOption> lArgOptions)
        {
            // Loop through the arguments and validate them
            foreach (ArgOption argOption in lArgOptions)
            {
                // All arguments require a long or short name
                if (String.IsNullOrEmpty(argOption.LongName) && String.IsNullOrEmpty(argOption.ShortName))
                {
                    throw new Exception("\n[!] Invalid argument option. All options must have either a long or short name.");
                }
                
                // All arguments require a value type
                if (argOption.ArgumentType == ValueType.None)
                {
                    throw new Exception("\n[!] Invalid argument option. All options must have a value type.");
                }
                
                // All arguments require a variable name
                if (String.IsNullOrEmpty(argOption.Name))
                {
                    throw new Exception("\n[!] Invalid argument option. All options must have a name.");
                }
            }
            
            // Return the list of arguments back to the caller
            return lArgOptions;
        }

        /// <summary>
        /// Pass command line arguments to the parser and get back a dictionary where the key value contains the parsed argument
        /// </summary>
        /// <param name="args">Takes the command line arguments</param>
        /// <param name="lArgOptions">Takes a list of ArgOption's</param>
        /// <returns>IDictionary</returns>
        public static IDictionary<String, Object> ParseArguments(String[] args, List<ArgOption> lArgOptions)
        {
            // Validate the arguments
            lArgOptions = ValidateArguments(lArgOptions);
            
            // Get the full string of the arguments
            String sArgs = GetStringFromArgs(args);
            
            // Create a new dynamic object
            IDictionary<String, Object> dArgs = new Dictionary<String, Object>();
            
            // Loop through the arguments and parse them
            foreach (ArgOption option in lArgOptions)
            {
                // We differentiate between boolean flags and other value types
                String sValue = String.Empty;
                if (option.ArgumentType == ValueType.Boolean)
                {
                    sValue = GetArgByRegex(sArgs, option.LongName, option.ShortName, true);
                }
                else
                {
                    sValue = GetArgByRegex(sArgs, option.LongName, option.ShortName);
                }

                // If the value is empty and the argument is required, throw an exception
                if (String.IsNullOrEmpty(sValue) && option.Required)
                {
                    throw new Exception("\n[!] Required argument missing: " + option.Name);
                }
                
                // If the value is not empty, set the value on the dynamic object
                if (!String.IsNullOrEmpty(sValue))
                {
                    // Process the value and assign it to the dynamic object
                    switch (option.ArgumentType)
                    {
                        case ValueType.String:
                            try
                            {
                                dArgs.Add(option.Name, sValue);
                            } catch (Exception ex)
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    ex.Message);
                            }
                            break;
                        case ValueType.Int32:
                            try
                            {
                                dArgs.Add(option.Name, Int32.Parse(sValue));
                            } catch (Exception ex)
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    ex.Message);
                            }
                            break;
                        case ValueType.Int64:
                            try
                            {
                                dArgs.Add(option.Name, Int64.Parse(sValue));
                            } catch (Exception ex)
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    ex.Message);
                            }
                            break;
                        case ValueType.UInt32:
                            try
                            {
                                dArgs.Add(option.Name, UInt32.Parse(sValue));
                            } catch (Exception ex)
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    ex.Message);
                            }
                            break;
                        case ValueType.UInt64:
                            try
                            {
                                dArgs.Add(option.Name, UInt64.Parse(sValue));
                            } catch (Exception ex)
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    ex.Message);
                            }
                            break;
                        case ValueType.Boolean:
                            try
                            {
                                Boolean bResult = true;
                                try
                                {
                                    bResult = Boolean.Parse(sValue);
                                } catch {}
                                dArgs.Add(option.Name, bResult);
                            } catch (Exception ex)
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    ex.Message);
                            }
                            break;
                        case ValueType.Enum:
                            // Is this a valid enum?
                            if (option.EnumValidator.IsEnum)
                            {
                                // Convert string to enum value
                                try
                                {
                                    dArgs.Add(option.Name, Enum.Parse(option.EnumValidator, sValue, true));
                                } catch (Exception ex)
                                {
                                    throw new Exception("\n[!] Error parsing argument:\n" + 
                                                        "Option : " + option.Name + "\n" +
                                                        "Value  : " + sValue + "\n" +
                                                        ex.Message);
                                }
                            }
                            else
                            {
                                throw new Exception("\n[!] Error parsing argument:\n" + 
                                                    "Option : " + option.Name + "\n" +
                                                    "Value  : " + sValue + "\n" +
                                                    "Invalid enum provided for validation.");
                            }
                            break;
                        default:
                            throw new Exception("\n[!] Argument type not defined: " + option.Name);
                    }
                }
            }
            
            // Return the dynamic object back to the caller
            return dArgs;
        }
    }
}