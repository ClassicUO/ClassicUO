#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Platforms.Windows
{
    public abstract class MessageHook : IDisposable
    {
        public abstract int HookType { get; }

        private readonly WndProcHandler m_Hook;
        private readonly IntPtr m_prevWndProc;
        private readonly IntPtr m_hIMC;

        public IntPtr HWnd { get; }

        protected MessageHook(IntPtr hWnd)
        {
            HWnd = hWnd;
            m_Hook = WndProcHook;
            m_prevWndProc = (IntPtr) NativeMethods.SetWindowLong(
                hWnd,
                NativeConstants.GWL_WNDPROC, (int) Marshal.GetFunctionPointerForDelegate(m_Hook));
            m_hIMC = NativeMethods.ImmGetContext(HWnd);
            //Application.AddMessageFilter(new InputMessageFilter(m_Hook));
        }

        ~MessageHook()
        {
            Dispose(false);
        }

        protected virtual IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case NativeConstants.WM_GETDLGCODE:
                    return (IntPtr) NativeConstants.DLGC_WANTALLKEYS;
                case NativeConstants.WM_IME_SETCONTEXT:
                    if ((int) wParam == 1)
                        NativeMethods.ImmAssociateContext(hWnd, m_hIMC);
                    break;
                case NativeConstants.WM_INPUTLANGCHANGE:
                    int rrr = (int) NativeMethods.CallWindowProc(m_prevWndProc, hWnd, msg, wParam, lParam);
                    NativeMethods.ImmAssociateContext(hWnd, m_hIMC);

                    return (IntPtr) 1;
            }

            return NativeMethods.CallWindowProc(m_prevWndProc, hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}