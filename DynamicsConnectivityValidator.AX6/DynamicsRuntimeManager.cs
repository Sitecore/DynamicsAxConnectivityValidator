// ------------------------------------------------------------------------------------------
// <copyright file="DynamicsRuntimeManager.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2016
// </copyright>
// ------------------------------------------------------------------------------------------
// Copyright 2016 Sitecore Corporation A/S
// Licensed under the SITECORE SHARED SOURCE LICENSE, you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       https://marketplace.sitecore.net/Shared_Source_License.aspx
// -------------------------------------------------------------------------------------------

namespace DynamicsConnectivityValidator
{
    using Microsoft.Dynamics.Commerce.Runtime;
    using Microsoft.Dynamics.Commerce.Runtime.Client;
    using Microsoft.Dynamics.Commerce.Runtime.Configuration;
    using Microsoft.Dynamics.Commerce.Runtime.Data;
    using Microsoft.Dynamics.Commerce.Runtime.DataModel;
    using Microsoft.Dynamics.Retail.Diagnostics;
    using System;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using ConfigurationException = Microsoft.Dynamics.Commerce.Runtime.ConfigurationException;

    /// <summary>
    ///     Manager to control access to directly calling CRT 
    /// </summary>   
    public class DynamicsRuntimeManager
    {
        private const long InvalidDefaultChannelId = 0;
        private const string KeyCrtConnectionString = "CommerceRuntimeConnectionString";
        private static CommerceRuntimeConfiguration _crtConfiguration;
        private static long _defaultChannelIdentifer = InvalidDefaultChannelId;   
        private CommerceRuntime _commerceRuntime;
        private ChannelManager _channelManager;
        private ProductManager _productManager;
        private CustomerManager _customerManager;
        private OrderManager _orderManager;

        /// <summary>
        ///     Gets the commerce runtime.
        /// </summary>      
        public CommerceRuntime CommerceRuntime
        {
            get { return _commerceRuntime ?? (_commerceRuntime = GetCommerceRuntime()); }
        }

        /// <summary>
        ///     Gets the channel manager.
        /// </summary>       
        public ChannelManager ChannelManager
        {
            get { return _channelManager ?? (_channelManager = ChannelManager.Create(CommerceRuntime)); }
        }

        /// <summary>
        ///     Gets the product manager.
        /// </summary>      
        public ProductManager ProductManager
        {
            get { return _productManager ?? (_productManager = ProductManager.Create(CommerceRuntime)); }
        }

        /// <summary>
        /// Gets the customer manager.
        /// </summary>      
        public CustomerManager CustomerManager
        {
            get { return _customerManager ?? (_customerManager = CustomerManager.Create(CommerceRuntime)); }
        }

        /// <summary>
        /// Gets the order manager.
        /// </summary>        
        public OrderManager OrderManager
        {
            get { return _orderManager ?? (_orderManager = OrderManager.Create(CommerceRuntime)); }
        }

        /// <summary>
        /// Gets or sets the gift card item identifier.
        /// </summary>       
        public string GiftCardItemId
        { get; set; }

        /// <summary>
        /// Gets or sets the product.
        /// </summary>        
        public Product Product { get; set; }

        /// <summary>
        /// Gets or sets the customer.
        /// </summary>       
        public Customer Customer { get; set; }
        
        /// <summary>
        /// Creates the cart.
        /// </summary>
        /// <returns>Cart with one line item.</returns>     
        public Cart CreateCart()
        {
            var shoppingCartId = GenerateTransactionId();
            var cart = new Cart
            {
                Id = shoppingCartId,
                CustomerId = this.Customer.AccountNumber,
                CartType = CartType.Checkout
            };
            OrderManager.CreateOrUpdateCart(cart, 0);

            //// get cart line from the product
            var variants = this.Product.CompositionInformation.VariantInformation.IndexedVariants;
            var variantId = variants.Keys.FirstOrDefault();

            var productVariant = variants[variantId];

            var cartLines = new Collection<CartLine>();
            var cartLine = new CartLine();

            var cartLineData = new CartLineData
            {
                ItemId = productVariant.ItemId,
                InventoryDimensionId = productVariant.InventoryDimensionId,
                ProductId = productVariant.DistinctProductVariantId,
                Quantity = 1,
                Comment = string.Empty,
                ["ProductDetails"] = string.Empty
            };

            cartLine.LineData = cartLineData;
            cartLines.Add(cartLine);

            var modes = CalculationModes.Totals | CalculationModes.Discounts | CalculationModes.Prices;

            return OrderManager.AddCartLines(shoppingCartId, this.Customer.AccountNumber, cartLines, new CalculationModes?(modes));
        }
        
        /// <summary>
        /// Gets the CommerceRuntime instance initialized by using the currently executing application's config.
        /// </summary>
        /// <returns>Commerce runtime instance.</returns>
        /// <remarks>
        /// Caches the commerce runtime configuration and default channel identifier.
        /// </remarks>
        private static CommerceRuntime GetCommerceRuntime()
        {
            if (_crtConfiguration == null)
            {
                _crtConfiguration = GetCrtConfiguration();
            }

            var runtime = GetCommerceRuntime(_crtConfiguration);

            return runtime;
        }

        /// <summary>
        /// Gets the commerce runtime configuration by using the currently executing application's config..
        /// </summary>
        /// <returns>The commerce runtime configuration.</returns>
        private static CommerceRuntimeConfiguration GetCrtConfiguration()
        {
            var crtConnectionString = GetCrtConnectionString();
            var commerceRuntimeSection = CommerceRuntimeConfigurationManager.GetConfigurationSection("commerceRuntime");
            var crtConfiguration = new CommerceRuntimeConfiguration(commerceRuntimeSection, crtConnectionString, true);

            return crtConfiguration;
        }    

        /// <summary>
        /// Get the commerce runtime connection string from the application config file.
        /// </summary>
        /// <returns>The commerce runtime connection string.</returns>
        private static string GetCrtConnectionString()
        {
            var crtConnectionString = ConfigurationManager.ConnectionStrings[KeyCrtConnectionString].ConnectionString;
            if (string.IsNullOrEmpty(crtConnectionString))
            {
                NetTracer.Error(
                    "The commerce runtime connection string '{0}' was not found in the application config.",
                    KeyCrtConnectionString);
            }

            return crtConnectionString;
        }

        /// <summary>
        /// Gets the commerce runtime based on the passed in commerce runtime configuration.
        /// </summary>
        /// <param name="commerceRuntimeConfiguration">The commerce runtime configuration.</param>
        /// <returns>An instance of commerce runtime.</returns>
        /// <exception cref="Microsoft.Dynamics.Commerce.Runtime.ConfigurationException">
        ///     The default channel identifier cannot be zero
        /// </exception>
        private static CommerceRuntime GetCommerceRuntime(CommerceRuntimeConfiguration commerceRuntimeConfiguration)
        {
            if (_defaultChannelIdentifer == InvalidDefaultChannelId)
            {
                if (!long.TryParse(ConfigurationManager.AppSettings["channelId"], out _defaultChannelIdentifer))
                {
                    using (var commerceRuntime = CommerceRuntime.Create(commerceRuntimeConfiguration, CommercePrincipal.AnonymousPrincipal))
                    {
                        var channelManager = ChannelManager.Create(commerceRuntime);
                        _defaultChannelIdentifer = channelManager.GetCurrentChannelId();                       
                    }

                    if (_defaultChannelIdentifer == InvalidDefaultChannelId)
                    {
                        var message = string.Format(
                            CultureInfo.InvariantCulture,
                            "The default channel identifier {0} was returned from CRT. Please ensure that a default operating unit number has been specified as part of the <commerceRuntime> configuration section.",
                            _defaultChannelIdentifer);
                        throw new ConfigurationException(ConfigurationErrors.InvalidChannelConfiguration, message);
                    }
                }               
            }

            var principal = new CommercePrincipal(new CommerceIdentity(_defaultChannelIdentifer, new[] {CommerceRoles.Storefront}));
            var runtime = CommerceRuntime.Create(commerceRuntimeConfiguration, principal);

            return runtime;
        }

        /// <summary>
        /// Generates the transaction identifier.
        /// </summary>
        /// <returns>A transaction identifier</returns>
        private static string GenerateTransactionId()
        {
            var buffer = new byte[0x20];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(buffer);
            }

            return Convert.ToBase64String(buffer);
        }
    }
}