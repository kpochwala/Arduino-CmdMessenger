﻿#region CmdMessenger - MIT - (c) 2014 Thijs Elenbaas.
/*
  CmdMessenger - library that provides command based messaging

  Permission is hereby granted, free of charge, to any person obtaining
  a copy of this software and associated documentation files (the
  "Software"), to deal in the Software without restriction, including
  without limitation the rights to use, copy, modify, merge, publish,
  distribute, sublicense, and/or sell copies of the Software, and to
  permit persons to whom the Software is furnished to do so, subject to
  the following conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  Copyright 2014 - Thijs Elenbaas
*/

#endregion

using System.Threading;

namespace CommandMessenger
{
    // Functionality comparable to AutoResetEvent (http://www.albahari.com/threading/part2.aspx#_AutoResetEvent)
    // but implemented using the monitor class: not inter processs, but ought to be more efficient.
    public class EventWaiter
    {
        public enum WaitState
        {
            Quit,
            TimeOut,
            Normal
        }

        readonly object _key = new object();
        bool _block;
        bool _quit;


        /// <summary>
        /// start blocked (waiting for signal)
        /// </summary>
        public EventWaiter() 
        {
            lock (_key)
            {
                _block = true;
                Monitor.Pulse(_key);
            }
        }

        /// <summary>
        /// start blocked or signalled. 
        /// </summary>
        /// <param name="set">If true, first Wait will directly continue</param>
        public EventWaiter(bool set)
        {
            lock (_key)
            {
                _block = !set;
                Monitor.Pulse(_key);
            }
        }

        /// <summary>
        /// Wait function. Blocks until signal is set or time-out
        /// </summary>
        /// <param name="timeOut">time-out in ms</param>
        /// <returns></returns>
        public WaitState WaitOne(int timeOut)
        {
            lock (_key)
            {
                // Check if quit has been raised before the wait function is entered
                if (_quit) { return WaitState.Quit; }

                // Check if signal has already been raised before the wait function is entered                
                if (!_block)
                {
                    // If so, reset event for next time and exit wait loop
                    _block = true;
                    return WaitState.Normal;
                }               

                // Wait under conditions
                bool noTimeOut = true;
                while (noTimeOut && _block)
                {
                    noTimeOut = Monitor.Wait(_key, timeOut);
                }
                // Block Wait for next entry
                _block = true;

                // Check if quit signal has already been raised after wait                
                if (_quit) { return WaitState.Quit; }

                // Return whether the Wait function was quit because of an Set event or timeout
                return noTimeOut ? WaitState.Normal : WaitState.TimeOut;
            }
        }

        /// <summary>
        /// Sets signal, will unblock thread in Wait function
        /// </summary>
        public void Set()
        {
            lock (_key)
            {
                _block = false;
                Monitor.Pulse(_key);
            }
        }

        /// <summary>
        /// Resets signal, will block threads entering Wait function
        /// </summary>
        public void Reset()
        {
            lock (_key)
            {
                _block = true;
            }
        }

        /// <summary>
        /// Quit. Unblocks thread in Wait function and exits
        // will not block again until Resume is called
        /// </summary>
        public void Quit()
        {
            lock (_key)
            {
                _block = false;
                _quit = true;
                Monitor.Pulse(_key);
            }
        }

        /// <summary>
        /// Resumes functionallity
        /// </summary>
        /// <param name="set">If true, first Wait will directly continue</param>
        public void Resume(bool set)
        {
            lock (_key)
            {
                _block = !set;
                _quit = false;
                Monitor.Pulse(_key);
            }
        }
    }
}
