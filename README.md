# UXSSHPush
Pushing Folder via SSH to RemoteServer

# Usage:
    Show this help:
      --help
    Be verbose during processing:
     --verbose
    Using specified configfile for processing:
     --config="C:\MyRepo\MyConfig.json"
    Create Empty Configuration:
     --create

# Sample:
UXSSHPush --config="C:\MyRepo\MyConfig.json" --create

# Sample Configfile:

    {
        "Hostname": "my.host.sample",
        "Port": 22,
        "Username": "MyUsername",
        "Password": "MySecret",
        "PrivateKeyFile": null,
        "Remotepath": "/home/user/sample",
        "Localpath": "C:\\MyRepo\\bin\\publish\\",
        "PreCommand": [
            "precommand1",
            "precommand2"
        ],
        "PostCommand": [
            "postcommand1",
            "postcommand2"
        ],
       "Excludefiles": [
           "appsettings.json",
           ".txt"
       ]
    }

# Add to File Publish Profile

    <Target Condition="" Name="CustomActionsAfterPublish" AfterTargets="AfterPublish">
        <Exec ConsoleToMSBuild="true" Command="UXSSHPush --config=&quot;PathToConfigFile&quot;">
            <Output TaskParameter="ConsoleOutput" PropertyName="OutputOfExec" />
        </Exec>
        <Message Text="@(OutputOfExec)"/>
    </Target>
