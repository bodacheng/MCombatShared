using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Json;
using System;

public class AppSetting
{
    public static AppSetting Value = new AppSetting();
    float _bgmVolume = 0.5f, _effectsVolume = 0.5f;

    public SystemLanguage Language { get; set; } = SystemLanguage.English;

    // Initializes the key codes.  Each key in your project should have a keycode defined here.  Add new keycodes here.  Delete unused ones.
    public KeyCode UpKeyCode;
    public KeyCode DownKeyCode;
    public KeyCode LeftKeyCode;
    public KeyCode RightKeyCode;
    public KeyCode JumpKeyCode;
    public KeyCode DreamComboKeyCode;
    public KeyCode Fire1KeyCode;
    public KeyCode Fire2KeyCode;
    public KeyCode Fire3KeyCode;

    private string lobbyTheme;

    public static async UniTask PlayBGM(string addressKey)
    {
        var clip = await AddressablesAssetLoader.LoadT<AudioClip>(
            addressKey,
            onFailure: key =>
            {
                Debug.LogWarning(AddressablesResourcePolicy.LoadFailureMessage(key));
                return UniTask.CompletedTask;
            });

        if (BGMSource.clip == clip)
        {
            return;
        }

        BGMSource.clip = clip;
        BGMSource.loop = true;
        BGMSource.Play();
    }

    private static AudioSource uiAudioSource;

    public static AudioSource UiAudioSource
    {
        get => uiAudioSource;
        set
        {
            if (value != null)
            {
                value.volume = AppSetting.Value._effectsVolume;
            }
            uiAudioSource = value;
        }
    }


    private static AudioSource bgmSource;
    public static AudioSource BGMSource
    {
        get => bgmSource;
        set
        {
            if (value != null)
            {
                value.volume = AppSetting.Value._bgmVolume;
            }
            bgmSource = value;
        }
    }

    public void Mute()
    {
        if (bgmSource != null)
            bgmSource.mute = true;
    }

    public void UnMute()
    {
        if (bgmSource != null)
            bgmSource.mute = false;
    }

    public float BgmVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Mathf.Clamp(value, 0, 1);
            if (BGMSource != null)
                BGMSource.volume = _bgmVolume;
        }
    }

    public float EffectsVolume
    {
        get => _effectsVolume;
        set
        {
            _effectsVolume = Mathf.Clamp(value, 0, 1);
            if (UiAudioSource != null)
                UiAudioSource.volume = _effectsVolume;
        }
    }

    public static void Save()
    {
        string json = JsonConvert.SerializeObject(Value);
        LocalJson.SaveToJsonFile_persistentDataPath(null, "AppSetting.json", json);
    }

    private static AppSetting CreateDefault()
    {
        return new AppSetting
        {
            Language = (Application.systemLanguage == SystemLanguage.ChineseSimplified ||
                        Application.systemLanguage == SystemLanguage.ChineseTraditional) ?
                SystemLanguage.Chinese : Application.systemLanguage,
            _bgmVolume = 0.5f, _effectsVolume = 0.5f,
        };
    }

    private static bool TryLoadFile(string path, out AppSetting setting)
    {
        setting = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            setting = JsonConvert.DeserializeObject<AppSetting>(File.ReadAllText(path));
            if (setting == null)
            {
                throw new JsonSerializationException("AppSetting.json contains no settings.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load settings from {path}: {ex.Message}");
            return false;
        }
    }

    private static void PreserveCorruptFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Move(path, path + ".corrupt-" + DateTime.UtcNow.Ticks);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not preserve damaged settings file: {ex.Message}");
        }
    }

    public static void Load()
    {
        var wholePath = Path.Combine(Application.persistentDataPath, "AppSetting.json");
        if (!TryLoadFile(wholePath, out var settings))
        {
            var backupPath = wholePath + ".bak";
            if (TryLoadFile(backupPath, out settings))
            {
                PreserveCorruptFile(wholePath);
                try
                {
                    File.Copy(backupPath, wholePath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not restore settings backup: {ex.Message}");
                }
            }
            else
            {
                PreserveCorruptFile(wholePath);
                settings = CreateDefault();
                Value = settings;
                Save();
            }
        }

        Value = settings;

        BOButton.SetPlaySeMethod(
            (x) =>
            {
                AudioClip clip = null;
                switch (x)
                {
                    case SeType.None:
                        return;
                    case SeType.Tap:
                        clip = CommonSetting.BtnTapSound;
                        break;
                    case SeType.Confirm:
                        clip = CommonSetting.BtnConfirmSound;
                        break;
                    case SeType.ExTab:
                        clip = CommonSetting.ExTabSound;
                        break;
                    default:
                        clip = CommonSetting.BtnTapSound;
                        break;
                }
                if (UiAudioSource != null)
                {
                    UiAudioSource.volume = AppSetting.Value.EffectsVolume;
                    UiAudioSource.PlayOneShot(clip);
                }
            }
        );
    }

    #region pc settings

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ResolutionWidth", Screen.width);
        PlayerPrefs.SetInt("ResolutionHeight", Screen.height);
        PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("ResolutionWidth") && PlayerPrefs.HasKey("ResolutionHeight"))
        {
            int width = PlayerPrefs.GetInt("ResolutionWidth");
            int height = PlayerPrefs.GetInt("ResolutionHeight");
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
#if !UNITY_IOS && !UNITY_ANDROID
            Screen.SetResolution(width, height, isFullscreen);
#endif
        }
    }
    #endregion

}
