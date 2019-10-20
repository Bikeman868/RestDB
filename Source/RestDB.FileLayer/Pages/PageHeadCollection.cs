using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RestDB.FileLayer.Pages
{
    internal class PageHeadCollection: IDisposable
    {
        private readonly IDictionary<ulong, PageHead> _pages;
        private readonly IFileSet _fileSet;
        private readonly IPagePool _pagePool;
        private readonly IStartupLog _startupLog;
        private readonly IErrorLog _errorLog;
        private readonly Thread _cleanupThread;

        private bool _disposing;

        public PageHeadCollection(
            IFileSet fileSet, 
            IStartupLog startupLog,
            IErrorLog errorLog,
            IPagePool pagePool)
        {
            _pages = new Dictionary<ulong, PageHead>();
            _fileSet = fileSet;
            _startupLog = startupLog;
            _errorLog = errorLog;
            _pagePool = pagePool;

            _cleanupThread = new Thread(() =>
            {
                _startupLog.WriteLine("Stale page clean up thread starting");

                while (!_disposing)
                {
                    try
                    {
                        Thread.Sleep(50);

                        // TODO: Delete pages that have not been touched for a while and
                        //       have no cached versions
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _errorLog.WriteLine("Exception in page collection cleanup thread. " + ex.Message, ex);
                    }
                }

                _startupLog.WriteLine("Stale page clean up thread exiting");
            })
            {
                IsBackground = true,
                Name = "Page collection cleanup",
                Priority = ThreadPriority.AboveNormal
            };

            _cleanupThread.Start();
        }

        public void Dispose()
        {
            _startupLog.WriteLine("Disposing of page head collection");
            _disposing = true;

            _cleanupThread.Join(200);

            lock (_pages)
            {
                foreach (var pageHead in _pages.Values)
                    pageHead.Dispose();

                _pages.Clear();
            }
        }

        public override string ToString()
        {
            return "page head collection from " + _fileSet;
        }

        /// <summary>
        /// Returns the page head for a page number. The page head is the had
        /// of a linked list of versions of that page
        /// </summary>
        /// <returns>Null if the page does not exist in storage</returns>
        public PageHead GetPageHead(ulong pageNumber, CacheHints hints)
        {
            PageHead pageHead;

            lock (_pages)
            {
                if (!_pages.TryGetValue(pageNumber, out pageHead))
                {
                    var page = GetFromFileSet(pageNumber);
                    if (page == null) return null;

                    pageHead = new PageHead(pageNumber, page);
                    _pages.Add(pageNumber, pageHead);
                }
            }

            return pageHead;
        }

        /// <summary>
        /// Creates a new page in storage. A page with this number must not already exist
        /// </summary>
        public IPage NewPage(ulong pageNumber)
        {
            using (var page = _pagePool.Get(pageNumber, true))
            {
                lock (_pages) _pages.Add(pageNumber, new PageHead(pageNumber, page));
                return page.Reference();
            }
        }

        private IPage GetFromFileSet(ulong pageNumber)
        {
            using (var page = _pagePool.Get(pageNumber))
            {
                if (_fileSet.Read(page)) return page.Reference();
            }
            return null;
        }

    }
}
