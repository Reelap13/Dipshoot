using UnityEngine;

namespace Settings
{
    public enum SettingType
    {
        UNKNOWN = 0,
        MOUSE_SENSITIVITY = 200,
        HORIZONTAL_LOOK_SCALE = 201,
        VERTICAL_LOOK_SCALE = 202,
        HORIZONTAL_LOOK_INVERT = 203,
        VERTICAL_LOOK_INVERT = 204,

        MASTER_VOLUME = 300,
        SFX_VOLUME = 301,
        MUSIC_VOLUME = 302,
        VOICE_VOLUME = 303,

        RESOLUTION = 400,
        FULLSCREEN = 401,
        V_SYNC = 402,
        FPS_LIMIT = 403,
        GAMMA_SCALE = 404,

        SHADOW_TYPE = 500,
        SHADOW_RESOLUTION = 501,
        SHADOW_CASCADES = 502,
        SHADOW_DISTANCE = 503,
        TEXTURE_RESOLUTION = 504,
        VFX_CULLING_BLAS = 505,
        LOD_BIAS = 506,
        LOD_REDUCTION = 507,


        NEW_SETTINGS
    }
}