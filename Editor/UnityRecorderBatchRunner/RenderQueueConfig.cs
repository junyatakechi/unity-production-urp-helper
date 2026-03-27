using System;

namespace JayT.UnityProductionUrpHelper.UnityRecorderBatchRunner
{
    [Serializable]
    public class RenderQueueConfig
    {
        public RenderQueueSettings settings;
        public RenderingItem[] renderingList;
    }

    // NOTE: Intentionally NOT named "RenderSettings" to avoid conflict with UnityEngine.RenderSettings
    [Serializable]
    public class RenderQueueSettings
    {
        public int targetFPS;
        public bool capFPS;
        /// <summary>"4K" (3840x2160), "2K" (2560x1440), "1080p" (1920x1080), "720p" (1280x720)</summary>
        public string resolution;
        /// <summary>"H264" or "ProRes"</summary>
        public string encoder;
        /// <summary>ProRes only. "ap4x"=4444XQ, "ap4h"=4444, "apch"=422HQ, "apcn"=422, "apcs"=422LT, "apco"=422Proxy</summary>
        public string proResCodec;
        public bool includeAudio;
    }

    [Serializable]
    public class RenderingItem
    {
        public string renderingId;
        public SceneSet scene;
        public FrameInterval frameInterval;
    }

    [Serializable]
    public class SceneSet
    {
        public string background;
        public string main;
        public string timeline;
    }

    [Serializable]
    public class FrameInterval
    {
        public int start;
        public int end;
    }
}