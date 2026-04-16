using IT4s.Rhythm.Generation.Skeleton.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    public interface ISkeletonBuilder
    {
        SkeletonPattern BuildSkeleton(SkeletonBuildRequest request);
    }
}
