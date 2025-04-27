// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Microsoft.OpenApi.Models;
// using Swashbuckle.AspNetCore.SwaggerGen;

// namespace App.Core.Config
// {
//     internal class FormFileOperationFilter : IOperationFilter
//     {
//         public void Apply(OpenApiOperation operation, OperationFilterContext context)
//         {
//             var requestBody = operation.RequestBody;

//             // Only apply to operations expecting multipart/form-data
//             if (requestBody?.Content == null || !requestBody.Content.ContainsKey("multipart/form-data"))
//             {
//                 return;
//             }

//             var multipartSchema = requestBody.Content["multipart/form-data"].Schema;

//             // Look for properties that are arrays (like our 'Files' list)
//             var arrayProperties = multipartSchema?.Properties?
//                 .Where(p => p.Value.Type == "array" && p.Value.Items != null)
//                 .ToList();

//             if (arrayProperties == null || !arrayProperties.Any())
//             {
//                 return;
//             }

//             foreach (var arrayProperty in arrayProperties)
//             {
//                 var itemSchema = arrayProperty.Value.Items;

//                 // Resolve the reference if the item schema is a reference
//                 if (itemSchema.Reference != null && context.SchemaRepository.Schemas.ContainsKey(itemSchema.Reference.Id))
//                 {
//                     itemSchema = context.SchemaRepository.Schemas[itemSchema.Reference.Id];
//                 }

//                 // Look for properties named 'File' (case-insensitive) within the item schema
//                 var fileProperty = itemSchema?.Properties?.FirstOrDefault(p =>
//                     p.Key.Equals("file", StringComparison.OrdinalIgnoreCase));

//                 // If a 'File' property is found, set its type and format correctly for file upload
//                 if (fileProperty?.Value != null)
//                 {
//                     fileProperty.Value.Type = "string";
//                     fileProperty.Value.Format = "binary"; // This tells Swagger UI to show the file upload button
//                 }
//             }
//         }
//     }
// }
