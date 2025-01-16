using System.Diagnostics;

public class SapiTextToSpeech{
    public void Speak(string text){
        string escapedText = text.Replace("'", "''");
        string command = $"Add-Type -AssemblyName System.Speech; " +
                         $"(New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{escapedText}')";

        Process.Start(new ProcessStartInfo{
            FileName = "powershell",
            Arguments = $"-Command \"{command}\"",
            RedirectStandardOutput = false,
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }
}
