using UnityEngine;

public class SLStartupSettings : MonoBehaviour
{
    public CoordConverter.ConverterMode ConverterMode;

    private void OnEnable()
    {
        CoordConverter.Mode = ConverterMode;
    }
}
