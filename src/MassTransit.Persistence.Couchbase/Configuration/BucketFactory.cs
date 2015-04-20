namespace MassTransit.Persistence.Couchbase.Configuration
{
    using System;
    using System.Linq;

    using global::Couchbase.Core;

    public static class BucketFactory
    {
        public static IBucket CreateBucketAndDesignDocument(
            ICluster cluster,
            string bucketName,
            string username,
            string password,
            string designDocumentName,
            string designDocumentBody)
        {
            var bucket = CreateBucket(
                cluster,
                bucketName,
                username,
                password);

            CreateDesignDocument(
                bucket,
                username,
                password,
                designDocumentName,
                designDocumentBody);

            return bucket;
        }

        public static IBucket CreateBucket(
            ICluster cluster,
            string bucketName,
            string username,
            string password)
        {
            var clusterManager = cluster.CreateManager(username, password);

            var buckets = clusterManager.ListBuckets();

            if (!buckets.Success)
            {
                throw new Exception(buckets.Message, buckets.Exception);
            }

            if (!buckets.Value.Any(b => b.Name.Equals(
                bucketName,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                var createBucketResult = clusterManager.CreateBucket(bucketName);
                
                if (!createBucketResult.Success)
                {
                    throw new Exception(
                        createBucketResult.Message,
                        createBucketResult.Exception);
                }
            }

            return cluster.OpenBucket(bucketName);
        }

        public static void CreateDesignDocument(
            IBucket bucket,
            string username,
            string password,
            string designDocumentName,
            string designDocumentBody)
        {
            var bucketManager = bucket.CreateManager(username, password);

            var designDocument = bucketManager.GetDesignDocument(designDocumentName);

            ////TODO:More detailed analysis of designDocument.Message required, there is JSON response
            if (designDocument.Success)
            {
                return;
            }

            var insertDesignDocumentResult = bucketManager.InsertDesignDocument(
                designDocumentName,
                designDocumentBody);

            if (!insertDesignDocumentResult.Success)
            {
                throw new Exception(
                    insertDesignDocumentResult.Message,
                    insertDesignDocumentResult.Exception);
            }
        }
    }
}
