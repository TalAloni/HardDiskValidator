/* Copyright (C) 2016 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DiskAccessLibrary;
using Utilities;

namespace HardDiskValidator
{
    public class DiskTester
    {
        const int TransferSizeLBA = 131072; // 64 MB (assuming 512-byte sectors)

        private TestName m_testName;
        private Disk m_disk;
        private bool m_abort;

        public delegate void UpdateStatusHandler(long currentPosition);
        public event UpdateStatusHandler OnStatusUpdate;
        public delegate void AddToLogHandler(string format, params object[] args);
        public event AddToLogHandler OnLogUpdate;

        public DiskTester(TestName testName, Disk disk)
        {
            m_testName = testName;
            m_disk = disk;
        }

        public BlockStatus PerformTest(long sectorIndex, long sectorCount)
        {
            if (m_testName == TestName.Read)
            {
                return PerformReadTest(sectorIndex, sectorCount);
            }
            else if (m_testName == TestName.ReadWipeDamagedRead)
            {
                return PerformReadWipeDamagedReadTest(sectorIndex, sectorCount);
            }
            else if (m_testName == TestName.ReadWriteVerifyRestore)
            {
                return PerformReadWriteVerifyRestoreTest(sectorIndex, sectorCount);
            }
            else if (m_testName == TestName.WriteVerify)
            {
                return PerformWriteVerifyTest(sectorIndex, sectorCount);
            }
            else
            {
                return PerformVerifyTest(sectorIndex, sectorCount);
            }
        }

        private BlockStatus PerformReadTest(long sectorIndex, long sectorCount)
        {
            // The only reason to read every sector separately is to maximize data recovery
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                int leftToRead = (int)(sectorCount - sectorOffset);
                int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                try
                {
                    ReadSectors(sectorIndex + sectorOffset, sectorsToRead);
                }
                catch (IOException ex)
                {
                    int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    if (errorCode == (int)Win32Error.ERROR_IO_DEVICE || errorCode == (int)Win32Error.ERROR_CRC)
                    {
                        AddToLog("Read failure at sectors {0:###,###,###,###,##0}-{1:###,###,###,###,##0}", sectorIndex, sectorIndex + sectorsToRead - 1);
                        return BlockStatus.Damaged;
                    }
                    else // ERROR_FILE_NOT_FOUND, ERROR_DEVICE_NOT_CONNECTED, ERROR_DEV_NOT_EXIST etc.
                    {
                        AddToLog("Read failure (Win32 error: {0}) at sectors {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode, sectorIndex, sectorIndex + sectorsToRead - 1);
                        return BlockStatus.IOError;
                    }
                }

                if (m_abort)
                {
                    return BlockStatus.Untested;
                }
            }
            return BlockStatus.OK;
        }

        private BlockStatus PerformReadWipeDamagedReadTest(long sectorIndex, long sectorCount)
        {
            bool crcErrorOccuredInFirstPass = false;
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += TransferSizeLBA)
            {
                int leftToRead = (int)(sectorCount - sectorOffset);
                int sectorsToRead = (int)Math.Min(leftToRead, TransferSizeLBA);
                List<long> damagedSectors;
                bool ioErrorOccured;
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                ReadEverySector(sectorIndex + sectorOffset, sectorsToRead, out damagedSectors, out ioErrorOccured);
                if (m_abort)
                {
                    return BlockStatus.Untested;
                }

                if (ioErrorOccured)
                {
                    return BlockStatus.IOError;
                }

                if (damagedSectors.Count > 0)
                {
                    crcErrorOccuredInFirstPass = true;
                    foreach (long damagedSectorIndex in damagedSectors)
                    {
                        byte[] sectorBytes = new byte[m_disk.BytesPerSector];
                        try
                        {
                            WriteSectors(damagedSectorIndex, sectorBytes);
                        }
                        catch (IOException ex)
                        {
                            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                            AddToLog("Write failure (Win32 error: {0}) at sector {1}", errorCode, damagedSectorIndex);
                            return BlockStatus.IOError;
                        }
                        AddToLog("Sector {0} has been overwritten", damagedSectorIndex);
                    }

                    // TODO: We should make sure that the writes are flushed to disk
                    try
                    {
                        ReadSectors(sectorIndex + sectorOffset, sectorsToRead);
                    }
                    catch (IOException ex)
                    {
                        int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                        if (errorCode == (int)Win32Error.ERROR_IO_DEVICE || errorCode == (int)Win32Error.ERROR_CRC)
                        {
                            AddToLog("Second read failure at sectors {0:###,###,###,###,##0}-{1:###,###,###,###,##0}", sectorIndex, sectorIndex + sectorsToRead - 1);
                            return BlockStatus.Damaged;
                        }
                        else // ERROR_FILE_NOT_FOUND, ERROR_DEVICE_NOT_CONNECTED, ERROR_DEV_NOT_EXIST etc.
                        {
                            AddToLog("Second read failure (Win32 error: {0}) at sectors {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode, sectorIndex, sectorIndex + sectorsToRead - 1);
                            return BlockStatus.IOError;
                        }
                    }
                }
                if (m_abort)
                {
                    return BlockStatus.Untested;
                }
            }

            if (!crcErrorOccuredInFirstPass)
            {
                return BlockStatus.OK;
            }
            else
            {
                return BlockStatus.OverwriteOK;
            }
        }

        private BlockStatus PerformReadWriteVerifyRestoreTest(long sectorIndex, long sectorCount)
        {
            bool crcErrorOccuredInFirstPass = false;
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += TransferSizeLBA)
            {
                int leftToRead = (int)(sectorCount - sectorOffset);
                int sectorsToRead = (int)Math.Min(leftToRead, TransferSizeLBA);
                List<long> damagedSectors;
                bool ioErrorOccured;
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                byte[] data = ReadEverySector(sectorIndex + sectorOffset, sectorsToRead, out damagedSectors, out ioErrorOccured);
                if (m_abort)
                {
                    return BlockStatus.Untested;
                }

                if (ioErrorOccured)
                {
                    return BlockStatus.IOError;
                }

                if (damagedSectors.Count > 0)
                {
                    crcErrorOccuredInFirstPass = true;
                }

                // TODO: generate dummy read operation if leftToRead < TransferSizeLBA
                BlockStatus writeVerifyStatus = PerformWriteVerifyTest(sectorIndex + sectorOffset, sectorsToRead);
                
                // restore the original data
                try
                {
                    WriteSectors(sectorIndex + sectorOffset, data);
                }
                catch (Exception ex)
                {
                    int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    AddToLog("Restore failure (Win32 error: {0}) at sectors {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode, sectorIndex, sectorIndex + sectorsToRead - 1);
                    return BlockStatus.IOError;
                }

                if (writeVerifyStatus != BlockStatus.OK)
                {
                    return writeVerifyStatus;
                }

                if (m_abort)
                {
                    return BlockStatus.Untested;
                }
            }

            if (crcErrorOccuredInFirstPass)
            {
                return BlockStatus.OverwriteOK;
            }
            else
            {
                return BlockStatus.OK;
            }
        }

        private BlockStatus PerformWriteVerifyTest(long sectorIndex, long sectorCount)
        {
            // When we perform a write operation and then immediately read it, we may get the result from the disk buffer and not from the disk surface (confirmed with a broken WD15EADS)
            // To be sure that we read from the disk surface, we first write the entire UI block, and only then read from it. 
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                int leftToRead = (int)(sectorCount - sectorOffset);
                int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                byte[] pattern = GetTestPattern(sectorIndex + sectorOffset, sectorsToRead, m_disk.BytesPerSector);
                try
                {
                    WriteSectors(sectorIndex + sectorOffset, pattern);
                }
                catch (IOException ex)
                {
                    int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    AddToLog("Write failure (Win32 error: {0}) at sectors {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode, sectorIndex, sectorIndex + sectorsToRead - 1);
                    return BlockStatus.IOError;
                }

                if (m_abort)
                {
                    return BlockStatus.Untested;
                }
            }

            return PerformVerifyTest(sectorIndex, sectorCount);
        }

        private BlockStatus PerformVerifyTest(long sectorIndex, long sectorCount)
        {
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                int leftToRead = (int)(sectorCount - sectorOffset);
                int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();

                byte[] pattern = GetTestPattern(sectorIndex + sectorOffset, sectorsToRead, m_disk.BytesPerSector);
                byte[] temp;
                try
                {
                    temp = ReadSectors(sectorIndex + sectorOffset, sectorsToRead);
                }
                catch (IOException ex)
                {
                    int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    if (errorCode == (int)Win32Error.ERROR_IO_DEVICE || errorCode == (int)Win32Error.ERROR_CRC)
                    {
                        AddToLog("Read failure at sectors {0:###,###,###,###,##0}-{1:###,###,###,###,##0}", sectorIndex, sectorIndex + sectorsToRead - 1);
                        return BlockStatus.Damaged;
                    }
                    else // ERROR_FILE_NOT_FOUND, ERROR_DEVICE_NOT_CONNECTED, ERROR_DEV_NOT_EXIST etc.
                    {
                        AddToLog("Read failure (Win32 error: {0}) at sectors {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode, sectorIndex, sectorIndex + sectorsToRead - 1);
                        return BlockStatus.IOError;
                    }
                }

                if (m_abort)
                {
                    return BlockStatus.Untested;
                }

                if (!ByteUtils.AreByteArraysEqual(pattern, temp))
                {
                    AddToLog("Verification mismatch at sectors {0:###,###,###,###,##0}-{1:###,###,###,###,##0}", sectorIndex, sectorIndex + sectorsToRead - 1);
                    return BlockStatus.Damaged;
                }
            }

            return BlockStatus.OK;
        }

        /// <returns>Will return null if test is aborted</returns>
        public byte[] ReadSectors(long sectorIndex, int sectorCount)
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

        public void WriteSectors(long sectorIndex, byte[] data)
        {
            int sectorCount = data.Length / m_disk.BytesPerSector;
            if (sectorCount > PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                // we must write one segment at the time
                for (int sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
                {
                    int leftToWrite = sectorCount - sectorOffset;
                    int sectorsToWrite = (int)Math.Min(leftToWrite, PhysicalDisk.MaximumDirectTransferSizeLBA);
                    byte[] segment = new byte[sectorsToWrite * m_disk.BytesPerSector];
                    Array.Copy(data, sectorOffset * m_disk.BytesPerSector, segment, 0, sectorsToWrite * m_disk.BytesPerSector);
                    long currentPosition = (sectorIndex + sectorOffset) * m_disk.BytesPerSector;
                    UpdateStatus(currentPosition);
                    m_disk.WriteSectors(sectorIndex + sectorOffset, segment);
                }
            }
            else
            {
                UpdateStatus(sectorIndex * m_disk.BytesPerSector);
                m_disk.WriteSectors(sectorIndex, data);
            }
        }

        /// <returns>Will return null if test is aborted</returns>
        private byte[] ReadEverySector(long sectorIndex, int sectorCount, out List<long> damagedSectors, out bool ioErrorOccured)
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

        private void UpdateStatus(long position)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(position);
            }
        }

        private void AddToLog(string format, params object[] args)
        {
            if (OnLogUpdate != null)
            {
                OnLogUpdate(format, args);
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
                m_abort = true;
            }
        }

        private static byte[] GetTestPattern(long sectorIndex, int sectorCount, int bytesPerSector)
        {
            byte[] pattern = new byte[sectorCount * bytesPerSector];
            for (int sectorOffset = 0; sectorOffset < sectorCount; sectorOffset++)
            {
                for (int offsetInSector = 0; offsetInSector <= bytesPerSector - 8; offsetInSector += 8)
                {
                    BigEndianWriter.WriteInt64(pattern, sectorOffset * bytesPerSector + offsetInSector, sectorIndex + sectorOffset);
                }
            }
            return pattern;
        }

        private static byte[] GetTestPattern(long sectorIndex, int bytesPerSector)
        {
            byte[] pattern = new byte[bytesPerSector];
            for (int offset = 0; offset <= bytesPerSector - 8; offset += 8)
            {
                BigEndianWriter.WriteInt64(pattern, offset, sectorIndex);
            }
            return pattern;
        }
    }
}
