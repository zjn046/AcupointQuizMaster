using Android.Views;
using Android.Widget;
using Android.Views.Accessibility;
using Android.OS;

namespace AcupointQuizMaster
{
    public class AdjustableViewAccessibilityDelegate : View.AccessibilityDelegate
    {
        private readonly TextView _view;
        private readonly Func<bool> _onIncrement;
        private readonly Func<bool> _onDecrement;
        private readonly Func<string> _getCurrentValue;
        private readonly string _description;

        private const int ActionIncrementId = 4096;  // AccessibilityNodeInfo.ActionScrollForward
        private const int ActionDecrementId = 8192;  // AccessibilityNodeInfo.ActionScrollBackward

        public AdjustableViewAccessibilityDelegate(
            TextView view,
            Func<bool> onIncrement,
            Func<bool> onDecrement,
            Func<string> getCurrentValue,
            string description)
        {
            _view = view;
            _onIncrement = onIncrement;
            _onDecrement = onDecrement;
            _getCurrentValue = getCurrentValue;
            _description = description;
        }

        public override void OnInitializeAccessibilityNodeInfo(View? host, AccessibilityNodeInfo? info)
        {
            base.OnInitializeAccessibilityNodeInfo(host, info);
            if (info == null || _view == null) return;

            // 设置为SeekBar类名，让TalkBack知道这是可调节的
            info.ClassName = Java.Lang.Class.FromType(typeof(SeekBar)).CanonicalName;

            // 设置内容描述
            var currentValue = _getCurrentValue();
            info.ContentDescription = $"{_description}：{currentValue}";

            // 添加增加和减少操作
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                info.AddAction(new AccessibilityNodeInfo.AccessibilityAction(ActionIncrementId, "向前滚动"));
                info.AddAction(new AccessibilityNodeInfo.AccessibilityAction(ActionDecrementId, "向后滚动"));
            }
            else
            {
                info.AddAction((global::Android.Views.Accessibility.Action)ActionIncrementId);
                info.AddAction((global::Android.Views.Accessibility.Action)ActionDecrementId);
            }

            // 设置为可聚焦和可点击
            info.Focusable = true;
            info.Clickable = true;
        }

        public override bool PerformAccessibilityAction(View? host, [Android.Runtime.GeneratedEnum] global::Android.Views.Accessibility.Action action, Bundle? arguments)
        {
            if (_view == null) 
                return base.PerformAccessibilityAction(host, action, arguments);

            bool handled = false;
            int actionId = (int)action;

            if (actionId == ActionIncrementId)
            {
                handled = _onIncrement();
            }
            else if (actionId == ActionDecrementId)
            {
                handled = _onDecrement();
            }

            if (handled)
            {
                // 播报新值
                var newValue = _getCurrentValue();
                _view.AnnounceForAccessibility($"{_description}：{newValue}");
                return true;
            }

            return base.PerformAccessibilityAction(host, action, arguments);
        }
    }
}