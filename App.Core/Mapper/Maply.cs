using System.Linq.Expressions;

namespace App.Core.Mapper
{
    public interface IMappingRegistration
    {
        void RegisterMappings();
    }
    
    public static class Maply
    {
        private static readonly Dictionary<(Type, Type), LambdaExpression> _mappings = new();

        public static void Register<TEntity, TDto>(Expression<Func<TEntity, TDto>> mapping)
        {
            _mappings[(typeof(TEntity), typeof(TDto))] = mapping;
        }

        public static Expression<Func<TEntity, TDto>> GetMapping<TEntity, TDto>()
        {
            var key = (typeof(TEntity), typeof(TDto));
            
            if (_mappings.TryGetValue(key, out var mapping))
                return (Expression<Func<TEntity, TDto>>)mapping;
            
            TryRegisterMapping<TDto>();
            
            if (_mappings.TryGetValue(key, out mapping))
                return (Expression<Func<TEntity, TDto>>)mapping;
            
            throw new InvalidOperationException($"Mapping for {typeof(TEntity)} to {typeof(TDto)} not found.");
        }
        
        private static void TryRegisterMapping<TDto>()
        {
            var dtoType = typeof(TDto);
            if (typeof(IMappingRegistration).IsAssignableFrom(dtoType) && !dtoType.IsAbstract)
            {
                try
                {
                    var instance = Activator.CreateInstance(dtoType) as IMappingRegistration;
                    instance?.RegisterMappings();
                }
                catch (Exception ex)
                {
                    // Log exception but continue
                    Console.WriteLine($"Failed to register mapping for {dtoType.Name}: {ex.Message}");
                }
            }
        }
    }

    public static class MaplyExtensions
    {
        public static IQueryable<TDto> ProjectTo<TSource, TDto>(this IQueryable<TSource> source)
        {
            var mapping = Maply.GetMapping<TSource, TDto>();
            return source.Select(mapping);
        }

        public static IEnumerable<TDto> MapTo<TSource, TDto>(this IEnumerable<TSource> source)
        {
            var mappingFunc = Maply.GetMapping<TSource, TDto>().Compile();
            return source.Select(mappingFunc);
        }
        
        public static TDto MapTo<TSource, TDto>(this TSource source)
        {
            var mappingFunc = Maply.GetMapping<TSource, TDto>().Compile();
            return mappingFunc(source);
        }
        
        public static List<TDto> MapToList<TSource, TDto>(this IEnumerable<TSource> source)
        {
            return source.MapTo<TSource, TDto>().ToList();
        }
    }
}

