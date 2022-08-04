using UnityEngine;

namespace SLUnity
{
    public class SLStartupSettings : MonoBehaviour
    {
        public CoordConverter.ConverterMode ConverterMode;

        private void OnEnable()
        {
            CoordConverter.Mode = ConverterMode;
        }
    }
}
