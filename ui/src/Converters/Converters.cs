using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BreezeLink.UI.Converters;

/// <summary>
/// 布尔值到颜色转换器
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Color.FromArgb(255, 16, 124, 16) : Color.FromArgb(255, 231, 72, 86);
        }

        return Color.FromArgb(255, 128, 128, 128);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到状态颜色转换器
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue
                ? new SolidColorBrush(Color.FromArgb(255, 16, 124, 16))
                : new SolidColorBrush(Color.FromArgb(255, 231, 72, 86));
        }

        return new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到指示器颜色转换器
/// </summary>
public class BoolToIndicatorColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Color.FromArgb(255, 16, 124, 16) : Color.FromArgb(255, 231, 72, 86);
        }

        return Color.FromArgb(255, 128, 128, 128);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值取反转换器
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return true;
    }
}

/// <summary>
/// 状态到字符串转换器
/// </summary>
public class StatusToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "运行中" : "已停止";
        }

        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接状态到字符串转换器
/// </summary>
public class ConnectionStatusToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "已连接" : "未连接";
        }

        return "未连接";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 数值到百分比转换器
/// </summary>
public class ValueToPercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double doubleValue)
        {
            return $"{doubleValue:F1}%";
        }

        if (value is int intValue)
        {
            return $"{intValue:F1}%";
        }

        return "0.0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 时间戳到字符串转换器
/// </summary>
public class TimestampToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件大小转换器
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long sizeInBytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (sizeInBytes >= GB)
            {
                return $"{sizeInBytes / (double)GB:F2} GB";
            }
            else if (sizeInBytes >= MB)
            {
                return $"{sizeInBytes / (double)MB:F2} MB";
            }
            else if (sizeInBytes >= KB)
            {
                return $"{sizeInBytes / (double)KB:F2} KB";
            }
            else
            {
                return $"{sizeInBytes} B";
            }
        }

        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 速度转换器
/// </summary>
public class SpeedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double speed)
        {
            if (speed >= 1000)
            {
                return $"{speed / 1000:F2} MB/s";
            }
            else
            {
                return $"{speed:F0} KB/s";
            }
        }

        return "0 KB/s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
