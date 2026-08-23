using UnityEngine.Playables;



public static class PlayableDirectorExtensions
{
    public static void SetPlaybackSpeed(this PlayableDirector director, float speed)
    {
        var playableGraph = director.playableGraph;
        if (!playableGraph.IsValid())
        {
            director.RebuildGraph();
            playableGraph = director.playableGraph;
        }

        for (int i = 0; i < playableGraph.GetRootPlayableCount(); ++i)
        {
            playableGraph.GetRootPlayable(i).SetSpeed(speed);
        }
    }



    public static void StopAndResetState(this PlayableDirector director)
    {
        if (director.playableGraph.IsValid())
        {
            director.time = 0;
            director.Evaluate();
        }
        director.Stop();
    }



    public static void Replay(this PlayableDirector director)
    {
        director.time = 0;
        director.Play();
    }
}
