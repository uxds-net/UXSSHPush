using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UXSSHPush
{
    class SSHOptions
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PrivateKeyFile { get; set; }
        public string Remotepath { get; set; }
        public string Localpath { get; set; }
        public List<string> PreCommand { get; set; }
        public List<string> PostCommand { get; set; }
        public List<string> Excludefiles { get; set; }

    }

    class SSHTool: IDisposable
    {
        readonly ConnectionInfo connectionInfo;
        readonly SSHOptions options;
        readonly SshClient sshclient;
        readonly SftpClient sftpclient;
        readonly bool verbose;
        public SSHTool(SSHOptions _options, bool _verbose)
        {
            this.options = _options;
            this.verbose = _verbose;

            AuthenticationMethod authentication;
            if (options.PrivateKeyFile != null)
            {
                authentication = new PrivateKeyAuthenticationMethod(options.Username,
                    new PrivateKeyFile[]
                    {
                        new PrivateKeyFile(options.PrivateKeyFile, options.Password)
                    }
                );
            }
            else 
            {
                authentication = new PasswordAuthenticationMethod(options.Username, options.Password);
            }

            connectionInfo = new ConnectionInfo(options.Hostname, options.Port, options.Username, authentication);

            sshclient = new SshClient(connectionInfo);
            sftpclient = new SftpClient(connectionInfo);
        }
        private void VerboseLog(string log)
        {
            if (verbose)
            {
                Console.WriteLine(log);
            }
        }
        private bool ExcludedFile(string filename)
        {
            foreach (string excl in options.Excludefiles)
            {
                if (excl.StartsWith('*'))
                {
                    if (filename.EndsWith(excl[1..], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else
                {
                    if (filename.Equals(excl, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void PreCommands()
        {
            sshclient.Connect();
            Console.WriteLine("Processing Pre Commands");
            foreach (string pre in options.PreCommand)
            {
                using (SshCommand cmd = sshclient.RunCommand(pre))
                {
                    VerboseLog(cmd.Result);
                }
            }
            sshclient.Disconnect();
        }
        public void PostCommands()
        {
            sshclient.Connect();
            Console.WriteLine("Processing Post Commands");
            foreach (string post in options.PostCommand)
            {
                using (SshCommand cmd = sshclient.RunCommand(post))
                {
                    VerboseLog(cmd.Result);
                }
            }
            sshclient.Disconnect();
        }

        public void ProcessFiles()
        {
            Console.WriteLine("Processing Files");
            sftpclient.Connect();
            UploadDirectory(options.Localpath, options.Remotepath);
            sftpclient.Disconnect();
        }
        private void UploadDirectory(string localPath, string remotePath)
        {
            VerboseLog("Uploading directory " + localPath + " to " + remotePath);

            IEnumerable<FileSystemInfo> infos = new DirectoryInfo(localPath).EnumerateFileSystemInfos();
            foreach (FileSystemInfo info in infos)
            {
                if (!sftpclient.Exists(remotePath))
                {
                    VerboseLog("Creating remote directory " + remotePath);
                    sftpclient.CreateDirectory(remotePath);
                }

                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    string subPath = remotePath + "/" + info.Name;
                    if (!sftpclient.Exists(subPath))
                    {
                        sftpclient.CreateDirectory(subPath);
                    }
                    UploadDirectory(info.FullName, remotePath + "/" + info.Name);
                }
                else
                {
                    if (!ExcludedFile(info.Name))
                    {
                        using (Stream fileStream = new FileStream(info.FullName, FileMode.Open))
                        {
                            VerboseLog("Uploading " + info.FullName + " " + ((FileInfo)info).Length + " bytes)");
                            sftpclient.UploadFile(fileStream, remotePath + "/" + info.Name);
                        }

                    }
                }
            }
        }
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            sftpclient.Dispose();
            sshclient.Dispose();
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
