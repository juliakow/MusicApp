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
            outputDevice.PlaybackStopped += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (isRepeatMode)
                    {
                        PlayCurrentTrack();
                    }
                    else
                    {
                        NextTrack();
                    }
                });
            };

            progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            progressTimer.Tick += (sender, e) => UpdateProgress();
        }

        private void PlayCurrentTrack()
        {
            if (songs.Count == 0 || currentTrackIndex < 0) return;

            try
            {

                outputDevice?.Stop();
                audioFile?.Dispose();


                audioFile = new AudioFileReader(songs[currentTrackIndex]);
                outputDevice.Init(audioFile);
                outputDevice.Play();

                // Aktualizacja UI
                UpdateSongInfo();
                progressTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odtwarzania: {ex.Message}");
            }
        }

        private void UpdateSongInfo()
        {
            var currentSong = songs[currentTrackIndex];
            SongTitle.Text = Path.GetFileNameWithoutExtension(currentSong);

            if (audioFile != null)
            {
                SongInfo.Text = $"Bitrate: {audioFile.WaveFormat.AverageBytesPerSecond * 8 / 1000}kbps | Sample rate: {audioFile.WaveFormat.SampleRate}Hz";
                ProgressBar.Maximum = audioFile.TotalTime.TotalSeconds;
            }
        }

        private void UpdateProgress()
        {
            if (audioFile != null)
            {
                ProgressBar.Value = audioFile.CurrentTime.TotalSeconds;
                CurrentTime.Text = $"{audioFile.CurrentTime:mm\\:ss} / {audioFile.TotalTime:mm\\:ss}";
            }
        }

        private void NextTrack()
        {
            if (isShuffleMode)
            {
                currentTrackIndex = new Random().Next(0, songs.Count);
            }
            else
            {
                currentTrackIndex = (currentTrackIndex + 1) % songs.Count;
            }
            PlayCurrentTrack();
        }

        private void PreviousTrack()
        {
            if (audioFile?.CurrentTime.TotalSeconds > 3)
            {
                audioFile.CurrentTime = TimeSpan.Zero;
            }
            else
            {

                currentTrackIndex = (currentTrackIndex - 1 + songs.Count) % songs.Count;
                PlayCurrentTrack();
            }
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
                MessageBox.Show($"Błąd odtwarzania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (outputDevice?.PlaybackState == PlaybackState.Paused)
            {
                outputDevice.Play();
            }
            else if (currentTrackIndex >= 0)
            {
                PlayCurrentTrack();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            outputDevice?.Pause();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) => NextTrack();
        private void PreviousButton_Click(object sender, RoutedEventArgs e) => PreviousTrack();

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            isRepeatMode = !isRepeatMode;
            RepeatButton.Content = isRepeatMode ? "🔂" : "🔁";
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            isShuffleMode = !isShuffleMode;
            ShuffleButton.Content = isShuffleMode ? "🔀 ON" : "🔀";
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
            RepeatButton.Content = isRepeatMode ? "🔂" : "🔁";
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            isShuffleMode = !isShuffleMode;
            ShuffleButton.Content = isShuffleMode ? "🔀 ON" : "🔀";
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
            return System.IO.Path.GetFileNameWithoutExtension(value?.ToString() ?? string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}