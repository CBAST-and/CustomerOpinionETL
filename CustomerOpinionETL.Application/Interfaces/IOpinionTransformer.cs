namespace CustomerOpinionETL.Application.Interfaces.Transformation;

using CustomerOpinionETL.Domain.Entities;
using CustomerOpinionETL.Domain.ValueObjects;

public interface IOpinionTransformer
{
    Task<Opinion> TransformAsync(OpinionRaw raw);
    Task<IEnumerable<Opinion>> TransformBatchAsync(IEnumerable<OpinionRaw> raws);
}