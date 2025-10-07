using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Denomica.Cosmos
{
    internal class UpsertItemResponse<T> : ItemResponse<T>
    {
        internal UpsertItemResponse(ItemResponse<T> sourceResponse, T resource)
        {
            _Headers = sourceResponse.Headers;
            _Resource = resource ?? sourceResponse.Resource;
            _StatusCode = sourceResponse.StatusCode;
        }

        private Headers _Headers;
        public override Headers Headers
        {
            get { return _Headers; }
        }

        private T _Resource;
        public override T Resource
        {
            get { return _Resource; }
        }

        private HttpStatusCode _StatusCode;
        public override HttpStatusCode StatusCode
        {
            get { return _StatusCode; }
        }


    }
}
