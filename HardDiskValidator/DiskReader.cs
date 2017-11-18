/* Copyright (C) 2016-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using DiskAccessLibrary;
using Utilities;

namespace HardDiskValidator
{
    public class DiskReader
    {
        private Disk m_disk;
        private bool m_abort;

        public delegate void UpdateStatusHandler(long currentPosition);
        public event UpdateStatusHandler OnStatusUpdate;
        public delegate void AddToLogHandler(string format, params object[] args);
        public event AddToLogHandler OnLogUpdate;

        public DiskReader(Disk disk)
        {
            m_disk = disk;
        }

        /// <returns>Will return null if test is aborted</returns>
        protected byte[] ReadSectors(long sectorIndex, int sectorCount)
        {
            if (sectorCount > PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                // we must read one segment at the time, and copy the segments to a big bufffer
                byte[] buffer = new byte[sectorCount * m_disk.BytesPerSector];
                for (int sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
                {
                    int leftToRead = sectorCount - sectorOffset;
                    int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                    long currentPosition = (sectorIndex + sectorOffset) * m_disk.BytesPerSector;
                    UpdateStatus(currentPosition);
                    byte[] segment = m_disk.ReadSectors(sectorIndex + sectorOffset, sectorsToRead);
                    Array.Copy(segment, 0, buffer, sectorOffset * m_disk.BytesPerSector, segment.Length);

                    if (m_abort)
                    {
                        return null;
                    }
                }
                return buffer;
            }
            else
            {
                UpdateStatus(sectorIndex * m_disk.BytesPerSector);
                return m_disk.ReadSectors(sectorIndex, sectorCount);
            }
        }

        /// <returns>Will return null if test is aborted</returns>
        protected byte[] ReadEverySector(long sectorIndex, int sectorCount, out List<long> damagedSectors, out bool ioErrorOccured)
        {
            if (sectorCount > PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                damagedSectors = new List<long>();
                ioErrorOccured = false;
                // we must read one segment at the time, and copy the segments to a big bufffer
                byte[] buffer = new byte[sectorCount * m_disk.BytesPerSector];
                for (int sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
                {
                    int leftToRead = sectorCount - sectorOffset;
                    int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                    List<long> damagedSectorsInSegment;
                    bool ioErrorOccuredInSegment;
                    byte[] segment = ReadEverySectorUnbuffered(sectorIndex + sectorOffset, sectorsToRead, out damagedSectorsInSegment, out ioErrorOccuredInSegment);
                    damagedSectors.AddRange(damagedSectorsInSegment);
                    if (ioErrorOccuredInSegment)
                    {
                        ioErrorOccured = true;
                    }
                    if (m_abort || ioErrorOccured)
                    {
                        return null;
                    }
                    Array.Copy(segment, 0, buffer, sectorOffset * m_disk.BytesPerSector, segment.Length);
                }
                return buffer;
            }
            else
            {
                return ReadEverySectorUnbuffered(sectorIndex, sectorCount, out damagedSectors, out ioErrorOccured);
            }
        }

        /// <returns>Will return null if test is aborted</returns>
        private byte[] ReadEverySectorUnbuffered(long sectorIndex, int sectorCount, out List<long> damagedSectors, out bool ioErrorOccured)
        {
            damagedSectors = new List<long>();
            ioErrorOccured = false;
            try
            {
                return ReadSectors(sectorIndex, sectorCount);
            }
            catch (IOException ex1)
            {
                int errorCode1 = System.Runtime.InteropServices.Marshal.GetHRForException(ex1);
                AddToLog("Read failure (Win32 error: {0}) at {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode1, sectorIndex, sectorIndex + sectorCount - 1);
                if (errorCode1 != (int)Win32Error.ERROR_IO_DEVICE && errorCode1 != (int)Win32Error.ERROR_CRC)
                {
                    ioErrorOccured = true;
                    return null;
                }

                byte[] data = new byte[sectorCount * m_disk.BytesPerSector];
                // Try to read sector by sector (to maximize data recovery)
                for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset++)
                {
                    long currentPosition = (sectorIndex + sectorOffset) * m_disk.BytesPerSector;
                    UpdateStatus(currentPosition);
                    try
                    {
                        byte[] sectorBytes = m_disk.ReadSector(sectorIndex + sectorOffset);
                        Array.Copy(sectorBytes, 0, data, sectorOffset * m_disk.BytesPerSector, m_disk.BytesPerSector);
                    }
                    catch (IOException ex2)
                    {
                        int errorCode2 = System.Runtime.InteropServices.Marshal.GetHRForException(ex2);
                        AddToLog("Read failure (Win32 error: {0}) at sector {1:###,###,###,###,##0}", errorCode2, sectorIndex + sectorOffset);
                        if (errorCode2 == (int)Win32Error.ERROR_IO_DEVICE || errorCode2 == (int)Win32Error.ERROR_CRC)
                        {
                            damagedSectors.Add(sectorIndex + sectorOffset);
                        }
                        else // ERROR_FILE_NOT_FOUND, ERROR_DEVICE_NOT_CONNECTED, ERROR_DEV_NOT_EXIST etc.
                        {
                            ioErrorOccured = true;
                        }
                    }
                    if (m_abort)
                    {
                        return null;
                    }
                }

                if (damagedSectors.Count == 0 && !ioErrorOccured)
                {
                    // Sometimes the bulk read will raise an exception but all sector by sector reads will succeed
                    ioErrorOccured = true;
                }
                return data;
            }
        }

        protected void UpdateStatus(long position)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(position);
            }
        }

        protected void AddToLog(string format, params object[] args)
        {
            if (OnLogUpdate != null)
            {
                OnLogUpdate(format, args);
            }
        }

        public Disk Disk
        {
            get
            {
                return m_disk;
            }
        }

        public bool Abort
        {
            get
            {
                return m_abort;
            }
            set
            {
                if (value)
                {
                    m_abort = true;
                }
                else
                {
                    throw new ArgumentException("Abort cannot be set to false");
                }
            }
        }
    }
}