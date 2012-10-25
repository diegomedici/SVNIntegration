using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using SharpSvn;

namespace SVNIntegration
{
    internal class Program
    {


        private static void Main(string[] args)
        {
            //SvnUpdateResult provides info about what happened during a checkout

            //we will use this to tell CheckOut() which revision to fetch


            //SvnCheckoutArgs wraps all of the options for the 'svn checkout' function
            SvnCheckOutArgs arguments = new SvnCheckOutArgs();
            bool tagOk = false;
            string tag = string.Empty;
            while (!tagOk)
            {
                Console.WriteLine("Tag: ");
                tag = Console.ReadLine();
                Regex twoDotPattern = new Regex("[0-9]*[.][0-9]*[.][0-9]*");
                if (string.IsNullOrEmpty(tag))
                {
                    Console.WriteLine("Tag vuoto.");
                }
                else
                {
                    if (!twoDotPattern.IsMatch(tag))
                    {
                        Console.WriteLine("Tag non corretto.");
                    }
                    else
                    {
                        tagOk = true;
                    }
                }
            }
            string reposUri = string.Format(ConfigurationManager.AppSettings["ReposUri"], tag);
            string defaultPath = ConfigurationManager.AppSettings["DefaultPath"];

            bool pathOk = false;
            string localPath = string.Empty;
            while (!pathOk)
            {
                try
                {
                    Console.WriteLine("Local path to checkout (default {0}): ", defaultPath);
                    string startingPath = Console.ReadLine();
                    if (string.IsNullOrEmpty(startingPath))
                    {
                        startingPath = defaultPath;
                    }
                    localPath = Path.Combine(startingPath, tag);

                    //Verifico se la dir è vuota
                    if(Directory.Exists(localPath))
                    {
                        string[] fileSystemEntries = Directory.GetFileSystemEntries(localPath);
                        if (fileSystemEntries.Length > 0)
                        {
                            throw new Exception("La directory non è vuota!");
                        }
                    }
                    pathOk = true;
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine("Errore: {0}", e.Message);
                    pathOk = false;
                }
            }

            Console.WriteLine("TAG: " + tag);
            Console.WriteLine("LOCAL PATH: " + localPath);
            Console.WriteLine("Repos URI: " + reposUri);
            

            Console.WriteLine("Prosegui? (Y/N)");
            if (Console.ReadLine().ToUpper() == "Y")
            {
                using (SvnClient client = new SvnClient())
                    try
                    {
                        //client.Processing +=new EventHandler<SvnProcessingEventArgs>(client_Processing);
                        //client.Committing +=new EventHandler<SvnCommittingEventArgs>(client_Committing);
                        //client.Progress += new EventHandler<SvnProgressEventArgs>(ClientProgress);
                        //SvnUriTarget is a wrapper class for SVN repository URIs
                        SvnUriTarget target = new SvnUriTarget(reposUri);
                        SvnUpdateResult result;
                        client.CheckOut(target, localPath, arguments, out result);
                        //client.Update(localPath, out result);
                        Console.WriteLine("OK: " + result);

                    }
                    catch (SvnException se)
                    {
                        Console.WriteLine(se.Message);
                        throw se;
                    }
                    catch (UriFormatException ufe)
                    {
                        Console.WriteLine(ufe.Message);
                        throw ufe;
                    }

                Console.WriteLine("Vuoi copiare i file di deploy sui server di produzione? (Y/N)");
                if (Console.ReadLine().ToUpper() == "Y")
                {
                    //Verifica dell'esistenza dei file di configurazione
                    string configurationFilesPath = Path.Combine(localPath, @"Build\ProductionConfigurations");
                    bool confOk = false;


                    string webDPCList = string.Empty;
                    while (!confOk)
                    {
                        try
                        {
                            Console.WriteLine("Elenco dei DPC separati da vigola: ");
                            webDPCList = Console.ReadLine();
                            foreach (string webDPCName in webDPCList.Split(','))
                            {
                                if (!File.Exists(Path.Combine(configurationFilesPath, webDPCName + ".xml")))
                                {
                                    throw new Exception(string.Format("Configurazione per il DPC {0} non trovata", webDPCName));
                                }
                            }
                            confOk = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            confOk = false;
                        }
                    }

                    try
                    {
                        Executor executorDeploy = new Executor();
                        executorDeploy.WorkingDirectory = localPath + @"\Build";
                        executorDeploy.FileName = String.Format(string.Format(@"{0}\Build\nant\bin\NAnt.exe", localPath));
                        executorDeploy.Arguments = "CopyFilesOnServerDeploy -D:WebDPCList=" + webDPCList;
                        executorDeploy.ErrorMatchString = "BUILD FAILED";
                        executorDeploy.Exec();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            Console.ReadLine();

        }

        private static void client_Committing(object sender, SvnCommittingEventArgs e)
        {
            var a = e.Items;
        }

        private static void client_Processing(object sender, SvnProcessingEventArgs e)
        {
            Console.WriteLine("Complete {0}", e.CommandType);
        }

        private static void ClientProgress(object sender, SvnProgressEventArgs e)
        {
            Console.WriteLine("Complete {0} {1}", e.Progress, e.TotalProgress);
        }

        //catch (Exception e)
        //{
        //    Console.WriteLine("Generic error: " + e.Message);
        //}
    }
}

