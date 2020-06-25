using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace musii.Music
{
    class PrivateMusic
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);
        public Queue<IMusicPlayback> MusicPlaylist = new Queue<IMusicPlayback>();
        
        private IMusicPlayback _currentSong = null;

        public IMusicPlayback CurrentSong()
        {
            return _currentSong;
        }

        public void QueueSong(IEnumerable<IMusicPlayback> songs)
        {
            semaphore.Wait();
            foreach (var k in songs)
            {
                MusicPlaylist.Enqueue(k);
            }
            semaphore.Release();
        }

        public IMusicPlayback[] GetQueue()
        {
            return MusicPlaylist.ToArray();
        }

        public IMusicPlayback PlayNext()
        {
            semaphore.Wait();
            if (MusicPlaylist.Count == 0)
            {
                semaphore.Release();
                _currentSong = null;
                return null;
            }

            _currentSong = MusicPlaylist.Dequeue();

            semaphore.Release();

            return _currentSong;
        }

        public IMusicPlayback PeekNext()
        {
            if (MusicPlaylist.Count == 0) return null;

            var next = MusicPlaylist.Peek();

            return next;
        }

        public int SkipSong(int count)
        {
            semaphore.Wait();
            if (_currentSong == null)
            {
                semaphore.Release();
                return 0;
            }
            int mCount = Math.Min(count-1, MusicPlaylist.Count);

            if (mCount >= 0)
            {
                _currentSong.IsSkipped = true;

                _currentSong.ShowSkipMessage = true;

                _currentSong.Stop();

                _currentSong = null;

                if (mCount > 0)
                {
                    for (int i = 0; i < mCount; i++)
                    {
                        MusicPlaylist.Dequeue();
                    }
                }
            }
            semaphore.Release();
            return mCount;
        }
    }
}
