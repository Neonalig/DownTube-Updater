using System.Diagnostics;
// ReSharper disable LoopCanBePartlyConvertedToQuery

static DirectoryInfo GetSub( DirectoryInfo Parent, string Child ) => new DirectoryInfo(Path.Combine(Parent.FullName, Child));
static FileInfo GetSubFile( DirectoryInfo Parent, string Child ) => new FileInfo(Path.Combine(Parent.FullName, Child));

FileInfo AppFile = new FileInfo(Environment.GetCommandLineArgs()[0]);
DirectoryInfo
    AppDir = AppFile.Directory!,  // expect .../DownTube/Updater
    MainDir = AppDir.Parent!, // expect .../DownTube
    ExDir = GetSub(AppDir, "_Update"); // expect .../DownTube/Updater/_Update/*.*

Console.WriteLine($"MainDir: {MainDir.FullName} ;; ExDir: {ExDir}");

if ( !ExDir.Exists ) { //No extracted update found so exit.
    Console.WriteLine("No update found.");
    Console.ReadKey();
    return;
}

foreach ( Process Proc in Process.GetProcesses() ) {
    try {
        //Console.WriteLine($"Some proc: {Proc.MainModule?.FileVersionInfo.FileName}");
        if ( Proc.MainModule?.FileVersionInfo.FileName is { } FN && FN.EndsWith("DownTube.exe", StringComparison.InvariantCultureIgnoreCase) ) {
            Console.WriteLine("DownTube.exe is running, killing process...");
            Proc.Kill();
            Thread.Sleep(500);
            break;
        }
    } catch { }
}

foreach ( FileInfo Local in MainDir.GetFiles() ) {
    if ( Local.Name.Equals("settings.json", StringComparison.InvariantCultureIgnoreCase) ) {
        Console.WriteLine($"Skipping {Local.Name}");
        continue;
    }
    Console.WriteLine($"Deleting {Local.Name}");
    Local.Delete();
}

static void ClearDirectory(DirectoryInfo Dir ) {
    foreach ( FileInfo Fl in Dir.GetFiles() ) {
        Fl.Delete();
    }
    foreach ( DirectoryInfo Di in Dir.GetDirectories() ) {
        ClearDirectory(Di);
        Di.Delete();
    }
}

foreach( DirectoryInfo Dir in MainDir.GetDirectories() ) {
    if ( Dir.Name.Equals(AppDir.Name, StringComparison.InvariantCultureIgnoreCase) ) {
        Console.WriteLine($"Skipping {Dir.Name}");
        continue;
    }
    ClearDirectory(Dir);
    Console.WriteLine($"Deleting {Dir.Name}");
    Dir.Delete();
}

foreach( FileInfo Fl in ExDir.GetFiles() ) {
    FileInfo Dest = GetSubFile(MainDir, Fl.Name);
    Console.WriteLine($"Copying {Fl.Name} to {Dest.FullName}");
    Fl.CopyTo(Dest.FullName);
}

static void CopyDirectory( DirectoryInfo Base, DirectoryInfo Dest ) {
    if ( !Dest.Exists ) { Dest.Create(); }
    foreach ( FileInfo Fl in Base.GetFiles() ) {
        FileInfo FlDest = GetSubFile(Dest, Fl.Name);
        Console.WriteLine($"\tCopying {Fl.Name} to {FlDest.FullName}");
        Fl.CopyTo(GetSubFile(Dest, Fl.Name).FullName);
    }
    foreach ( DirectoryInfo Di in Base.GetDirectories() ) {
        DirectoryInfo DiDest = GetSub(Dest, Di.Name);
        Console.WriteLine($"\tCopying {Di.Name} to {DiDest.FullName}");
        CopyDirectory(Di, DiDest);
    }
}

foreach ( DirectoryInfo Di in ExDir.GetDirectories() ) {
    DirectoryInfo Dest = GetSub(MainDir, Di.Name);
    Console.WriteLine($"Copying {Di.Name} to {Dest.FullName}");
    CopyDirectory(Di, Dest);
}

Console.WriteLine("Process complete.");
Process.Start(GetSubFile(MainDir, "DownTube.exe").FullName);