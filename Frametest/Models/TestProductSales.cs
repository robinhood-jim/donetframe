using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frametest.Models
{
    [MappingEntity("t_product_sales")]
    public class TestProductSales : BaseEntity
    {
        [MappingField(ifPrimary: true, ifIncrement: true)]
        public long Id
        {
            get; set;
        }
        public long ProductId
        {
            get; set;
        }
        public long SalesId
        {
            get; set;
        }
        public long BrandId
        {
            get; set;
        }
        public long CustomerId
        {
            get; set;
        }
        public long StoreId
        {
            get; set;
        }
        public int Nums
        {
            get; set;
        }
        public double Prices
        {
            get; set;
        }
        public double SalePrices
        {
            get; set;
        }
        public int Status
        {
            get; set;
        }
        public int PreSaleTag
        {
            get; set;
        }

    }
}
