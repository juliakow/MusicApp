using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace MusicPlayer
{
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private List<string> songs = new List<string>();
        private int currentTrackIndex = -1;
        private bool isRepeatMode = false;
        private bool isShuffleMode = false;
        private DispatcherTimer progressTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializePlayer();
            LoadMusicFiles();
        }

        private void InitializePlayer()
        {
            outputDevice = new WaveOutEvent();
            outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(200);
            progressTimer.Tick += ProgressTimer_Tick;

            VolumeSlider.Value = 50;
        }

        private void LoadMusicFiles()
        {
            string musicFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");
            if (Directory.Exists(musicFolder))
            {
                songs.AddRange(Directory.GetFiles(musicFolder, "*.mp3"));
                Playlist.ItemsSource = songs;
                if (songs.Count > 0)
                {
                    currentTrackIndex = 0;
                    Playlist.SelectedIndex = 0;
                }
            }
        }

        private void PlayCurrentTrack()
        {
            if (songs.Count == 0 || currentTrackIndex < 0 || currentTrackIndex >= songs.Count) return;

            try
            {
                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }

                audioFile = new AudioFileReader(songs[currentTrackIndex]);
                outputDevice.Init(audioFile);
                outputDevice.Play();

                SongTitle.Text = Path.GetFileNameWithoutExtension(songs[currentTrackIndex]);
                SongInfo.Text = $"Bitrate: {audioFile.WaveFormat.AverageBytesPerSecond * 8 / 1000} kbps | Sample rate: {audioFile.WaveFormat.SampleRate / 1000} kHz";
                ProgressBar.Maximum = audioFile.TotalTime.TotalSeconds;
                ProgressBar.Value = 0;

                progressTimer.Start();
                UpdateTimeDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing track: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTimeDisplay()
        {
            if (audioFile != null)
            {
                CurrentTime.Text = $"{audioFile.CurrentTime:mm\\:ss} / {audioFile.TotalTime:mm\\:ss}";
                ProgressBar.Value = audioFile.CurrentTime.TotalSeconds;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (songs.Count == 0) return;

            if (outputDevice?.PlaybackState == PlaybackState.Stopped && audioFile == null)
            {
                PlayCurrentTrack();
            }
            else
            {
                outputDevice?.Play();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            outputDevice?.Pause();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioFile != null)
            {
                audioFile.CurrentTime = TimeSpan.Zero;
                UpdateTimeDisplay();
                if (outputDevice?.PlaybackState == PlaybackState.Paused)
                {
                    outputDevice.Play();
                }
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (songs.Count == 0) return;

            if (audioFile?.CurrentTime.TotalSeconds > 3)
            {
                RestartButton_Click(sender, e);
            }
            else
            {
                if (isShuffleMode)
                {
                    currentTrackIndex = new Random().Next(0, songs.Count);
                }
                else if (currentTrackIndex > 0)
                {
                    currentTrackIndex--;
                }
                else
                {
                    currentTrackIndex = songs.Count - 1;
                }

                PlayCurrentTrack();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (songs.Count == 0) return;

            if (isShuffleMode)
            {
                currentTrackIndex = new Random().Next(0, songs.Count);
            }
            else if (currentTrackIndex < songs.Count - 1)
            {
                currentTrackIndex++;
            }
            else
            {
                currentTrackIndex = 0;
            }

            PlayCurrentTrack();
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            isRepeatMode = !isRepeatMode;
            RepeatButton.Content = isRepeatMode ? "🔂 Repeat" : "🔁 Repeat";
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            isShuffleMode = !isShuffleMode;
            ShuffleButton.Content = isShuffleMode ? "🔀 Shuffle ON" : "🔀 Shuffle";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (outputDevice != null)
            {
                outputDevice.Volume = (float)VolumeSlider.Value / 100;
            }
        }

        private void Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Playlist.SelectedIndex >= 0 && Playlist.SelectedIndex != currentTrackIndex)
            {
                currentTrackIndex = Playlist.SelectedIndex;
                PlayCurrentTrack();
            }
        }

        private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (isRepeatMode)
                {
                    RestartButton_Click(null, null);
                    outputDevice?.Play();
                }
                else
                {
                    NextButton_Click(null, null);
                }
            });
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDisplay();
        }

        protected override void OnClosed(EventArgs e)
        {
            progressTimer?.Stop();
            outputDevice?.Dispose();
            audioFile?.Dispose();
            base.OnClosed(e);
        }
    }

    public class PathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            return System.IO.Path.GetFileNameWithoutExtension(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}