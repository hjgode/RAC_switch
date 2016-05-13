using System;

using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;

using DWORD = System.Int32;

namespace batterylog2
{
    class BatteryStatusEx
    {
        #region pinvokes
        [DllImport("coredll.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int GetSystemPowerStatusEx2(SYSTEM_POWER_STATUS_EX2 pSystemPowerStatusEx2, [MarshalAs(UnmanagedType.U4), In] uint dwLen, [MarshalAs(UnmanagedType.Bool), In] bool fUpdate);

        [Serializable]
        private class SYSTEM_POWER_STATUS_EX2
        {
            /// <summary>
            /// AC power status.
            /// </summary>
            public byte ACLineStatus = 0;
            /// <summary>
            /// Battery charge status
            /// </summary>
            public byte BatteryFlag = 0;
            /// <summary>
            /// Percentage of full battery charge remaining. This member can be a value in the range 0 to 100, 
            /// or BATTERY_PERCENTAGE_UNKNOWN if the status is unknown. All other values are reserved.
            /// </summary>
            public byte BatteryLifePercent = 0;
            public byte Reserved1=0;
            /// <summary>
            /// Number of seconds of battery life remaining, or BATTERY_LIFE_UNKNOWN if remaining seconds are unknown.
            /// </summary>
            public DWORD BatteryLifeTime = 0;
            /// <summary>
            /// Number of seconds of battery life when at full charge, or BATTERY_LIFE_UNKNOWN if full battery lifetime is unknown.
            /// </summary>
            public DWORD BatteryFullLifeTime = 0; //
            public byte Reserved2=0;
            public byte BackupBatteryFlag=0;
            public byte BackupBatteryLifePercent=0;
            public byte Reserved3=0;
            public DWORD BackupBatteryLifeTime=0;
            public DWORD BackupBatteryFullLifeTime=0;
            /// <summary>
            /// Amount of battery voltage in millivolts (mV). This member can have a value in the range of 0 to 65,535.
            /// </summary>
            public DWORD BatteryVoltage = 0;
            /// <summary>
            /// Amount of instantaneous current drain in milliamperes (mA). This member can have a value in the range of 0 to 32,767 
            /// for charge, or 0 to –32,768 for discharge.
            /// </summary>
            public DWORD BatteryCurrent = 0;
            /// <summary>
            /// Short-term average of device current drain (mA). This member can have a value in the range 
            /// of 0 to 32,767 for charge, or 0 to –32,768 for discharge.
            /// </summary>
            public DWORD BatteryAverageCurrent = 0;
            /// <summary>
            /// Time constant in milliseconds (ms) of integration used in reporting BatteryAverageCurrent.
            /// </summary>
            public DWORD BatteryAverageInterval = 0;
            /// <summary>
            /// Long-term cumulative average discharge in milliamperes per hour (mAH). This member can have a value in 
            /// the range of 0 to –32,768. This value can be reset by charging or changing the batteries.
            /// </summary>
            public DWORD BatterymAHourConsumed=0;
            /// <summary>
            /// Battery temperature in degrees Celsius. This member can have a value in the range of –3,276.8 to 3,276.7; 
            /// the increments are 0.1 degrees Celsius.
            /// </summary>
            public DWORD BatteryTemperature=0;
            public DWORD BackupBatteryVoltage=0;
            public byte BatteryChemistry=0;
        }
        public enum ACLineStatus : byte
        {
            Offline = 0,
            Online = 1,
            BackUp = 2,
            Unknown = 255,
        }
        /*
        #define AC_LINE_OFFLINE                 0x00
        #define AC_LINE_ONLINE                  0x01
        #define AC_LINE_BACKUP_POWER            0x02
        #define AC_LINE_UNKNOWN                 0xFF
        */
        public enum BatteryFlag : byte{
            BATTERY_FLAG_HIGH               = 0x01,
            BATTERY_FLAG_LOW                = 0x02,
            BATTERY_FLAG_CRITICAL           = 0x04,
            BATTERY_FLAG_CHARGING           = 0x08,
            BATTERY_FLAG_NO_BATTERY         = 0x80,
            BATTERY_FLAG_UNKNOWN            = 0xFF,
        }
        const Int32 BATTERY_PERCENTAGE_UNKNOWN     = 0xFF;
        const UInt32 BATTERY_LIFE_UNKNOWN        = 0xFFFFFFFF;

        #endregion
        #region fields
        private SYSTEM_POWER_STATUS_EX2 m_SYSTEM_POWER_STATUS_EX2;

        public bool _isACpowered{
            get
            {
                if (updateStatus())
                    if ((ACLineStatus)m_SYSTEM_POWER_STATUS_EX2.ACLineStatus == ACLineStatus.Online)
                        return true;
                    else
                        return false;
                else
                    return false;   //dont know
            }
        }
        /// <summary>
        /// BatteryLifePercent
        /// Percentage of full battery charge remaining. This member can be a value in the range 0 to 100, or 
        /// BATTERY_PERCENTAGE_UNKNOWN if the status is unknown. All other values are reserved.
        /// </summary>
        public byte _lifePercent
        {
            get
            {
                if (updateStatus())
                    return m_SYSTEM_POWER_STATUS_EX2.BatteryLifePercent;
                else
                    return BATTERY_PERCENTAGE_UNKNOWN;
            }
        }

        /// <summary>
        /// BatteryLifeTime
        ///    Number of seconds of battery life remaining, or BATTERY_LIFE_UNKNOWN if remaining seconds are unknown.
        /// </summary>
        public Int32 _lifeTime
        {
            get
            {
                if (updateStatus())
                    return m_SYSTEM_POWER_STATUS_EX2.BatteryLifeTime;
                else
                {
                    return unchecked ((Int32) BATTERY_LIFE_UNKNOWN);
                }
            }
        }

        public Int32 _BatteryCurrent
        {
            get
            {
                if (updateStatus())
                    return (int)m_SYSTEM_POWER_STATUS_EX2.BatteryCurrent;
                else
                    return 0;
            }
        }
        public Int32 _BatteryAverageCurrent
        {
            get
            {
                if (updateStatus())
                    return m_SYSTEM_POWER_STATUS_EX2.BatteryAverageCurrent;
                else
                    return 0;
            }
        }
        public Int32 _BatteryAverageInterval
        {
            get
            {
                if (updateStatus())
                    return m_SYSTEM_POWER_STATUS_EX2.BatteryAverageInterval;
                else
                    return 0;
            }
        }
        public Int32 _BatterymAHourConsumed
        {
            get
            {
                if (updateStatus())
                    return m_SYSTEM_POWER_STATUS_EX2.BatterymAHourConsumed;
                else
                    return 0;
            }
        }
        
        #endregion

        public BatteryStatusEx()
        {
            updateStatus();
        }
        private bool updateStatus()
        {
            bool bRes = false;
            m_SYSTEM_POWER_STATUS_EX2 = new SYSTEM_POWER_STATUS_EX2();
            if (GetSystemPowerStatusEx2(m_SYSTEM_POWER_STATUS_EX2, (uint)Marshal.SizeOf(m_SYSTEM_POWER_STATUS_EX2), false) == (uint)Marshal.SizeOf(m_SYSTEM_POWER_STATUS_EX2))
            {
                bRes = true;
            }
            else
            {
                bRes = false;
            }
            return bRes;
        }
    }
}
