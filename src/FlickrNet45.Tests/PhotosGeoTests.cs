﻿using System;
using System.Linq;
using FlickrNet;
using NUnit.Framework;

namespace FlickrNet45.Tests
{
    /// <summary>
    /// Summary description for PhotosGeoTests
    /// </summary>
    [TestFixture]
    public class PhotosGeoTests : BaseTest
    {
        [Test]
        public void PhotoInfoParseFull()
        {
            string x = "<photo id=\"7519320006\">"
                    + "<location latitude=\"54.971831\" longitude=\"-1.612683\" accuracy=\"16\" context=\"0\" place_id=\"Ke8IzXlQV79yxA\" woeid=\"15532\">"
                    + "<neighbourhood place_id=\"Ke8IzXlQV79yxA\" woeid=\"15532\">Central</neighbourhood>"
                    + "<locality place_id=\"DW0IUrFTUrO0FQ\" woeid=\"20928\">Gateshead</locality>"
                    + "<county place_id=\"myqh27pQULzLWcg7Kg\" woeid=\"12602156\">Tyne and Wear</county>"
                    + "<region place_id=\"2eIY2QFTVr_DwWZNLg\" woeid=\"24554868\">England</region>"
                    + "<country place_id=\"cnffEpdTUb5v258BBA\" woeid=\"23424975\">United Kingdom</country>"
                    + "</location>"
                    + "</photo>";

            System.IO.StringReader sr = new System.IO.StringReader(x);
            System.Xml.XmlTextReader xr = new System.Xml.XmlTextReader(sr);
            xr.Read();

            var info = new PhotoInfo();
            ((IFlickrParsable)info).Load(xr);

            Assert.AreEqual("7519320006", info.PhotoId);
            Assert.IsNotNull(info.Location);
            Assert.AreEqual((GeoAccuracy)16, info.Location.Accuracy);

            Assert.IsNotNull(info.Location.Country);
            Assert.AreEqual("cnffEpdTUb5v258BBA", info.Location.Country.PlaceId);
        }

        [Test]
        public void PhotoInfoLocationParseShortTest()
        {
            string x = "<photo id=\"7519320006\">"
                + "<location latitude=\"-23.32\" longitude=\"-34.2\" accuracy=\"10\" context=\"1\" />"
                + "</photo>";

            System.IO.StringReader sr = new System.IO.StringReader(x);
            System.Xml.XmlTextReader xr = new System.Xml.XmlTextReader(sr);
            xr.Read();

            var info = new PhotoInfo();
            ((IFlickrParsable)info).Load(xr);

            Assert.AreEqual("7519320006", info.PhotoId);
            Assert.IsNotNull(info.Location);
            Assert.AreEqual((GeoAccuracy)10, info.Location.Accuracy);

        }

        [Test]
        [Category("AccessTokenRequired")]
        public void PhotosGetGetLocationTest()
        {
            var photos = AuthInstance.PhotosSearch(new PhotoSearchOptions() { HasGeo = true, UserId = Data.UserId, Extras = PhotoSearchExtras.Geo });

            var photo = photos.First();

            Console.WriteLine(photo.PhotoId);

            var location = AuthInstance.PhotosGeoGetLocation(photo.PhotoId);

            Assert.AreEqual(photo.Longitude, location.Longitude, "Longitudes should match exactly.");
            Assert.AreEqual(photo.Latitude, location.Latitude, "Latitudes should match exactly.");
        }

        [Test]
        [ExpectedException(typeof(FlickrApiException))]
        [Category("AccessTokenRequired")]
        public void PhotosGetGetLocationNullTest()
        {
            var photos = AuthInstance.PhotosSearch(new PhotoSearchOptions() { HasGeo = false, UserId = Data.UserId, Extras = PhotoSearchExtras.Geo });

            var photo = photos.First();

            var location = AuthInstance.PhotosGeoGetLocation(photo.PhotoId);

            Assert.IsNull(location, "Location should be null.");
        }

        [Test]
        [Category("AccessTokenRequired")]
        public void PhotosGetCorrectLocationTest()
        {
            var photo = AuthInstance.PhotosSearch(new PhotoSearchOptions() { HasGeo = true, UserId = Data.UserId, Extras = PhotoSearchExtras.Geo }).First();

            AuthInstance.PhotosGeoCorrectLocation(photo.PhotoId, photo.PlaceId, null);
        }

        [Test]
        [Category("AccessTokenRequired")]
        public void PhotosGeoSetContextTest()
        {
            var photo = AuthInstance.PhotosSearch(new PhotoSearchOptions() { HasGeo = true, UserId = Data.UserId, Extras = PhotoSearchExtras.Geo }).First();

            Assert.IsTrue(photo.GeoContext.HasValue, "GeoContext should be set.");

            var origContext = photo.GeoContext.Value;

            var newContext = origContext == GeoContext.Indoors ? GeoContext.Outdoors : GeoContext.Indoors;

            try
            {
                AuthInstance.PhotosGeoSetContext(photo.PhotoId, newContext);
            }
            finally
            {
                AuthInstance.PhotosGeoSetContext(photo.PhotoId, origContext);
            }
        }

        [Test]
        [Category("AccessTokenRequired")]
        public void PhotosGeoSetLocationTest()
        {
            var photo = AuthInstance.PhotosSearch(new PhotoSearchOptions() { HasGeo = true, UserId = Data.UserId, Extras = PhotoSearchExtras.Geo }).First();

            var origGeo = new { photo.Longitude, photo.Latitude, photo.Accuracy, Context = photo.GeoContext.Value };
            var newGeo = new { Latitude = -23.32, Longitude = -34.2, Accuracy = GeoAccuracy.Level10, Context = GeoContext.Indoors };

            try
            {
                AuthInstance.PhotosGeoSetLocation(photo.PhotoId, newGeo.Latitude, newGeo.Longitude, newGeo.Accuracy, newGeo.Context);

                var location = AuthInstance.PhotosGeoGetLocation(photo.PhotoId);
                Assert.AreEqual(newGeo.Latitude, location.Latitude, "New Latitude should be set.");
                Assert.AreEqual(newGeo.Longitude, location.Longitude, "New Longitude should be set.");
                Assert.AreEqual(newGeo.Context, location.Context, "New Context should be set.");
                Assert.AreEqual(newGeo.Accuracy, location.Accuracy, "New Accuracy should be set.");
            }
            finally
            {
                AuthInstance.PhotosGeoSetLocation(photo.PhotoId, origGeo.Latitude, origGeo.Longitude, origGeo.Accuracy, origGeo.Context);
            }
            
        }

        [Test]
        [Category("AccessTokenRequired")]
        public void PhotosGeoPhotosForLocationBasicTest()
        {
            var o = new PhotoSearchOptions
                                       {
                                           UserId = Data.UserId,
                                           HasGeo = true,
                                           PerPage = 1,
                                           Extras = PhotoSearchExtras.Geo
                                       };

            var photos = AuthInstance.PhotosSearch(o);
            var photo = photos[0];

            var photos2 = AuthInstance.PhotosGeoPhotosForLocation(photo.Latitude, photo.Longitude, photo.Accuracy, PhotoSearchExtras.All, 0, 0);

            Assert.IsNotNull(photos2, "PhotosGeoPhotosForLocation should not return null.");
            Assert.IsTrue(photos2.Count > 0, "Should return one or more photos.");

            foreach (var p in photos2)
            {
                Assert.IsNotNull(p.PhotoId);
                Assert.AreNotEqual(0, p.Longitude);
                Assert.AreNotEqual(0, p.Latitude);
            }

        }
    }
}
