using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Host_Process_for_Windows_Tasks
{
    class AudioPlayer
    {

        public enum Style
        {
            lowercase, uppercase, capslock
        }

        public static Boolean Blocked { get; set; } = false;

        private static int _playerInstanceCount = 0; //how many sounds are played atm
        
        public int ConsecutiveInterruptions { get; set; } = 0; //how often was another sound started while something was still playing consecutively



        public void playKey(Keys key, Style style)
        {
            String ressource = style.ToString() + "." + key.ToString() + ".wav";
            playFile(ressource);
        }


        public void saySomething(String something) //maybe use this as fallback if no sample is found? it has a completed event, so it could be integrated
        {
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            synthesizer.Volume = 100;  // 0...100
            synthesizer.Rate = -2;     // -10...10
            // Asynchronous
            synthesizer.SpeakAsync(something);
        }

        public void playInterruption()
        {
            playFileBlocking("ChillDude.wav");
        }


        private void playFileBlocking(String ressource)
        {
            Task.Run(() => playFileTask(ressource, true));
        }

        private void playFile(String ressource) 
        {
            
            Task.Run(() => playFileTask(ressource, false));
        }

        private void playFileTask(String ressource, Boolean blocking) //ressource relative to namespace.sounds, e.g. normal.A.wav
        {
            if (Blocked)
            {
                System.Console.WriteLine("Playback is blocked.");
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Host_Process_for_Windows_Tasks.sounds." + ressource;


                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if(stream == null)
                    {
                        System.Console.WriteLine(resourceName + " does not exist.");
                        return;
                    }

                    using (CSCore.IWaveSource soundSource = new WaveFileReader(stream))
                    {
                        
                        using (ISoundOut soundOut = GetSoundOut())
                        {
                            //Tell the SoundOut which sound it has to play
                            soundOut.Initialize(soundSource);

                            if (blocking)
                            {
                                Blocked = true;
                                soundOut.Stopped += delegate { Blocked = false; };
                            }

                            if (_playerInstanceCount > 0)
                            {
                                ConsecutiveInterruptions++;
                            }
                            else
                            {
                                ConsecutiveInterruptions = 0;
                            }

                            _playerInstanceCount++;
                            soundOut.Stopped += delegate { _playerInstanceCount--; };

                            //Play the sound
                            soundOut.Play();


                            Thread.Sleep(2000);

                            //Stop the playback
                            soundOut.Stop();
                        }
                    }
                }
            }
                
            
        }


        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }


    }

    

}
