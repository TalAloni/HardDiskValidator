/* Copyright (C) 2016-2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
using DiskAccessLibrary.Win32;
using Utilities;

namespace HardDiskValidator
{
    public class DiskTester : DiskReader
    {
        const int TransferSizeLBA = 131072; // 64 MB (assuming 512-byte sectors)

        private TestName m_testName;

        public DiskTester(TestName testName, Disk disk) : base(disk)
        {
            m_testName = testName;
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
            else if (m_testName == TestName.Write)
            {
                return PerformWriteTest(sectorIndex, sectorCount);
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
                long leftToRead = sectorCount - sectorOffset;
                int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                bool ioErrorOccured;
                byte[] segment = ReadSectors(sectorIndex + sectorOffset, sectorsToRead, out ioErrorOccured);
                if (Abort)
                {
                    return BlockStatus.Untested;
                }

                if (ioErrorOccured)
                {
                    return BlockStatus.IOError;
                }
                
                if (segment == null)
                {
                    return BlockStatus.Damaged;
                }
            }
            return BlockStatus.OK;
        }

        private BlockStatus PerformReadWipeDamagedReadTest(long sectorIndex, long sectorCount)
        {
            bool crcErrorOccuredInFirstPass = false;
            // We use a large TransferSizeLBA to circumvent the disk caching mechanism
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += TransferSizeLBA)
            {
                long leftToRead = sectorCount - sectorOffset;
                int sectorsToRead = (int)Math.Min(leftToRead, TransferSizeLBA);
                List<long> damagedSectors;
                bool ioErrorOccured;
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                ReadEverySector(sectorIndex + sectorOffset, sectorsToRead, out damagedSectors, out ioErrorOccured);
                if (Abort)
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
                        byte[] sectorBytes = new byte[Disk.BytesPerSector];
                        try
                        {
                            WriteSectors(damagedSectorIndex, sectorBytes);
                        }
                        catch (IOException ex)
                        {
                            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                            AddToLog("Write failure (Win32 error: {0}) at sector {1:###,###,###,###,##0}", errorCode, damagedSectorIndex);
                            return BlockStatus.IOError;
                        }
                        AddToLog("Sector {0:###,###,###,###,##0} has been overwritten", damagedSectorIndex);
                    }

                    // TODO: We should make sure that the writes are flushed to disk
                    byte[] segment = ReadSectors(sectorIndex + sectorOffset, sectorsToRead, out ioErrorOccured);
                    
                    if (Abort)
                    {
                        return BlockStatus.Untested;
                    }

                    if (ioErrorOccured)
                    {
                        return BlockStatus.IOError;
                    }

                    if (segment == null)
                    {
                        return BlockStatus.Damaged;
                    }
                }
                if (Abort)
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
                long leftToRead = sectorCount - sectorOffset;
                int sectorsToRead = (int)Math.Min(leftToRead, TransferSizeLBA);
                List<long> damagedSectors;
                bool ioErrorOccured;
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                byte[] data = ReadEverySector(sectorIndex + sectorOffset, sectorsToRead, out damagedSectors, out ioErrorOccured);
                if (Abort)
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

                if (Abort)
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
                long leftToRead = sectorCount - sectorOffset;
                int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                byte[] pattern = GetTestPattern(sectorIndex + sectorOffset, sectorsToRead, Disk.BytesPerSector);
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

                if (Abort)
                {
                    return BlockStatus.Untested;
                }
            }

            return PerformVerifyTest(sectorIndex, sectorCount);
        }

        private BlockStatus PerformWriteTest(long sectorIndex, long sectorCount)
        {
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                int leftToWrite = (int)(sectorCount - sectorOffset);
                int sectorsToWrite = (int)Math.Min(leftToWrite, PhysicalDisk.MaximumDirectTransferSizeLBA);
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();
                byte[] pattern = GetTestPattern(sectorIndex + sectorOffset, sectorsToWrite, Disk.BytesPerSector);
                try
                {
                    WriteSectors(sectorIndex + sectorOffset, pattern);
                }
                catch (IOException ex)
                {
                    int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    AddToLog("Write failure (Win32 error: {0}) at sectors {1:###,###,###,###,##0}-{2:###,###,###,###,##0}", errorCode, sectorIndex, sectorIndex + sectorsToWrite - 1);
                    return BlockStatus.IOError;
                }

                if (Abort)
                {
                    return BlockStatus.Untested;
                }
            }

            return BlockStatus.OK;
        }

        private BlockStatus PerformVerifyTest(long sectorIndex, long sectorCount)
        {
            bool verificationMismatch = false;
            for (long sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                long leftToRead = sectorCount - sectorOffset;
                int sectorsToRead = (int)Math.Min(leftToRead, PhysicalDisk.MaximumDirectTransferSizeLBA);
                // Clear allocations from previous iteration
                GC.Collect();
                GC.WaitForPendingFinalizers();

                bool ioErrorOccured;
                byte[] buffer = ReadSectors(sectorIndex + sectorOffset, sectorsToRead, out ioErrorOccured);

                if (Abort)
                {
                    return BlockStatus.Untested;
                }

                if (ioErrorOccured)
                {
                    return BlockStatus.IOError;
                }

                if (buffer == null)
                {
                    return BlockStatus.Damaged;
                }

                for (int position = 0; position < sectorsToRead; position++)
                {
                    byte[] pattern = GetTestPattern(sectorIndex + sectorOffset + position, Disk.BytesPerSector);
                    byte[] sectorBytes = ByteReader.ReadBytes(buffer, position * Disk.BytesPerSector, Disk.BytesPerSector);
                    if (!ByteUtils.AreByteArraysEqual(pattern, sectorBytes))
                    {
                        verificationMismatch = true;
                        AddToLog("Verification mismatch at sector {0:###,###,###,###,##0}", sectorIndex + sectorOffset + position);
                    }
                }
            }

            if (verificationMismatch)
            {
                return BlockStatus.Damaged;
            }
            else
            {
                return BlockStatus.OK;
            }
        }

        public void WriteSectors(long sectorIndex, byte[] data)
        {
            int sectorCount = data.Length / Disk.BytesPerSector;
            if (sectorCount > PhysicalDisk.MaximumDirectTransferSizeLBA)
            {
                // we must write one segment at the time
                for (int sectorOffset = 0; sectorOffset < sectorCount; sectorOffset += PhysicalDisk.MaximumDirectTransferSizeLBA)
                {
                    int leftToWrite = sectorCount - sectorOffset;
                    int sectorsToWrite = (int)Math.Min(leftToWrite, PhysicalDisk.MaximumDirectTransferSizeLBA);
                    byte[] segment = new byte[sectorsToWrite * Disk.BytesPerSector];
                    Array.Copy(data, sectorOffset * Disk.BytesPerSector, segment, 0, sectorsToWrite * Disk.BytesPerSector);
                    long currentPosition = (sectorIndex + sectorOffset) * Disk.BytesPerSector;
                    UpdateStatus(currentPosition);
                    Disk.WriteSectors(sectorIndex + sectorOffset, segment);
                }
            }
            else
            {
                UpdateStatus(sectorIndex * Disk.BytesPerSector);
                Disk.WriteSectors(sectorIndex, data);
            }
        }

        private static byte[] GetTestPattern(long sectorIndex, int bytesPerSector)
        {
            return GetTestPattern(sectorIndex, 1, bytesPerSector);
        }

        private static byte[] GetTestPattern(long sectorIndex, int sectorCount, int bytesPerSector)
        {
            byte[] buffer = new byte[sectorCount * bytesPerSector];
            for (int sectorOffset = 0; sectorOffset < sectorCount; sectorOffset++)
            {
                byte[] pattern = BigEndianConverter.GetBytes(sectorIndex + sectorOffset);
                for (int offsetInSector = 0; offsetInSector <= bytesPerSector - 8; offsetInSector += 8)
                {
                    Array.Copy(pattern, 0, buffer, sectorOffset * bytesPerSector + offsetInSector, 8);
                }
            }
            return buffer;
        }
    }
}
