using Common;

namespace ProcessingModule
{
    public class AlarmProcessor
    {

		public AlarmType GetAlarmForAnalogPoint(double eguValue, IConfigItem configItem)
        {
            if (eguValue < configItem.LowLimit)
            {
                return AlarmType.LOW_ALARM;
            }
            if (eguValue > configItem.HighLimit)
            {
                return AlarmType.HIGH_ALARM;
            }
            return AlarmType.NO_ALARM;
        }

		public AlarmType GetAlarmForDigitalPoint(ushort state, IConfigItem configItem)
        {
            if (state == configItem.AbnormalValue)
            {
                return AlarmType.ABNORMAL_VALUE;
            }
            return AlarmType.NO_ALARM;
        }
    }
}
