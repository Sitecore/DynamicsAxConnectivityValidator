// ------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Sitecore Corporation">
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
    using Microsoft.Dynamics.Commerce.Runtime.DataModel;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// General console Program for DynamicsConnectivityValidator
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {          
            CombinedWriter writer;
            var oldOut = Console.Out;

            try
            {                
                writer = new CombinedWriter("./DynamicsConnectivityValidator.log", false, Encoding.Unicode, 0x400, Console.Out);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open DynamicsConnectivityValidator.log for writing");
                Console.WriteLine(e.Message);
                return;
            }

            Console.SetOut(writer);
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            (se, cert, chain, sslerror) =>
            {
                return true;
            };

            var success = true;
            writer.Write("Dynamics Connectivity validation starting...");
            var crtManager = new DynamicsRuntimeManager();

            if (crtManager.CommerceRuntime == null)
            {
                success = false;
                writer.Write("Failed to create CommerceRuntime");
            }

            if (success)
            {
                try
                {
                    writer.Write("Validating the Channel manager...");
                    success = ChannelValidation(crtManager, writer);

                    writer.Write("Validating the Catalog manager...");
                    success = success && CatalogValidation(crtManager, writer);

                    writer.Write("Validating the Customer manager...");
                    success = success && CreateUser(crtManager, writer);

                    if (success)
                    {
                        writer.Write("Validating the Order manager...");
                        success = CreateOrder(crtManager, writer);
                    }
                }
                catch (Exception e)
                {
                    writer.Write(e.Message);
                    writer.Write(e.InnerException);
                    return;
                }
            }

            writer.Write(success
                ? "Dynamics Connectivity validation succeeded"
                : "Dynamics Connectivity validation failed");

            Console.SetOut(oldOut);
            writer.Close();        
        }

        /// <summary>
        /// Channels the validation.
        /// </summary>
        /// <param name="runtimeManager">The runtime manager.</param>
        /// <param name="writer">The writer.</param>
        /// <returns>True if the channel is valid, false otherwise</returns>
        private static bool ChannelValidation(DynamicsRuntimeManager runtimeManager, CombinedWriter writer)
        {
            var status = true;
            var channelId = runtimeManager.CommerceRuntime.CurrentPrincipal.ChannelId; 

            if (runtimeManager.ChannelManager == null)
            {
                writer.Write("Failed to create ChannelManager");               
                return false;
            }

            var channelConfiguration = runtimeManager.ChannelManager.GetChannelConfiguration();
            if (channelConfiguration == null)
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, "Unable to get channel configuration for the channel {0}", channelId));
                status = false;
            }
            else
            {
                writer.Write("ChannelCountryRegionISOCode = " + channelConfiguration.ChannelCountryRegionISOCode);
                writer.Write("Currency = " + channelConfiguration.Currency);
                writer.Write("CompanyCurrency = " + channelConfiguration.CompanyCurrency);
                writer.Write("CountryRegionId = " + channelConfiguration.CountryRegionId);
                writer.Write("DefaultLanguageId = " + channelConfiguration.DefaultLanguageId);
                writer.Write("TimeZoneInfoId = " + channelConfiguration.TimeZoneInfoId);
                writer.Write("GiftCardItemId = " + channelConfiguration.GiftCardItemId);
                runtimeManager.GiftCardItemId = channelConfiguration.GiftCardItemId;
            }
            
            var categories = runtimeManager.ChannelManager.GetChannelCategoryHierarchy(channelId, new QueryResultSettings());
            if (categories == null || categories.Results.Count == 0)
            {
                writer.Write("No categories were found");
                status = false;
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, "{0} categories were found", categories.Results.Count));
            }
            
            return status;
        }

        /// <summary>
        /// Catalogs the validation.
        /// </summary>
        /// <param name="runtimeManager">The runtime manager.</param>
        /// <param name="writer">The writer.</param>
        /// <returns>True is the catalog is valid, false otherwise</returns>
        private static bool CatalogValidation(DynamicsRuntimeManager runtimeManager, CombinedWriter writer)
        {
            var status = true;
            var channelId = runtimeManager.CommerceRuntime.CurrentPrincipal.ChannelId; 
            if (runtimeManager.ProductManager == null)
            {
                writer.Write("Failed to create ProductManager");               
                return false;
            }

            var catalogs = runtimeManager.ProductManager.GetProductCatalogs( channelId, new QueryResultSettings());
            if (catalogs == null || catalogs.Results.Count == 0)
            {
                writer.Write("No catalogs were found");
                status = false;
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, "{0} catalogs were found", catalogs.Results.Count)); 
            }           

            var products = runtimeManager.ProductManager.GetProducts(new QueryResultSettings{ Paging = new PagingInfo{ Top = 1000, CalculateRecordCount = true}});
            if (products == null || products.Results.Count == 0)
            {
                writer.Write("No products were found");
                status = false;
            }
            else
            {
                // find a product wich is not a gift card and not master product
                foreach (var product in products.Results)
                {
                    if (!product.IsMasterProduct || product.ItemId == runtimeManager.GiftCardItemId || !product.GetVariants().Any())
                    {
                        continue;
                    }

                    runtimeManager.Product = product;
                    break;
                }

                if (runtimeManager.Product == null)
                {
                    writer.Write("There is no Variant product which is not a girft card");
                }

                writer.Write(
                    products.Results.Count < 1000
                    ? string.Format(CultureInfo.InvariantCulture, "{0} products were found", products.Results.Count)
                    : "1000 or more products were found");
            }

            return status;
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="runtimeManager">The runtime manager.</param>
        /// <param name="writer">The writer.</param>
        /// <returns>True if the user was created, false otherwise</returns>
        private static bool CreateUser(DynamicsRuntimeManager runtimeManager, CombinedWriter writer)
        {
            var newCustomer = new Customer 
            { 
                Email = "test@demo.com", 
                FirstName = "Dynamics", 
                LastName = "Validation", 
                Language = CultureInfo.CurrentUICulture.ToString() 
            };

            var axCustomer = runtimeManager.CustomerManager.CreateCustomer(newCustomer);
            if (axCustomer == null)
            {
                writer.Write("Create Customer failed");               
                return false;
            }

            runtimeManager.Customer = axCustomer;
            writer.Write(string.Format(CultureInfo.InvariantCulture, "Customer with Account Number {0} has been successfully created", axCustomer.AccountNumber));

            return true;
        }

        /// <summary>
        /// Creates the order.
        /// </summary>
        /// <param name="runtimeManager">The runtime manager.</param>
        /// <param name="writer">The writer.</param>
        /// <returns>True if the order was created, false otherwise</returns>
        private static bool CreateOrder(DynamicsRuntimeManager runtimeManager, CombinedWriter writer)
        {
            var status = true;           
            
            // create cart           
            var cart = runtimeManager.CreateCart();
            if (cart == null)
            {
                writer.Write("Failed to Add Items to cart");
                return false;
            }

            writer.Write("Shopping cart with one line item has been created");

            // set OrderDeliveryOption      
            var deliveryAddress = new Address()
            {
                StreetNumber = "123",               
                Street = "Main",
                City = "Redmond", 
                State = "WA",                          
                ZipCode = "98052",
                ThreeLetterISORegionName = "USA",
                Email = "test@demo.com",            
                AddressTypeValue = (int)AddressType.None,
                Name = "Home"            
            };

            cart = runtimeManager.OrderManager.UpdateCartShippingAddress(cart.Id, runtimeManager.Customer.AccountNumber, deliveryAddress, "99", CalculationModes.All, string.Empty);
            
            if (cart == null)
            {
               writer.Write("Failed to update cart ShippingAddress");
               status = false;
            }
            else
            {
                var paymentCard = new PaymentCard
                {
                    Address1 = "12 street",
                    City = "Orlando",
                    Zip = "32822",
                    Country = "USA",
                    CardNumber = "4111111111111111",
                    CardTypes = "Visa",
                    CCID = "123",
                    ExpirationMonth = 11,
                    ExpirationYear = 2020,
                    NameOnCard = "john doe",
                };
          
                // add payment    
                var channelConfiguration = runtimeManager.ChannelManager.GetChannelConfiguration();
                IEnumerable<TenderType> tenderTypes = runtimeManager.ChannelManager.GetChannelTenderTypes(new QueryResultSettings()).Results;
                if (!tenderTypes.Any())
                {
                    writer.Write("No ChannelTenderTypes defined for the channel");
                    status = false;
                }

                IEnumerable<TenderType> creditCardTenderTypes = tenderTypes.Where(t => t.OperationId == (int)RetailOperation.PayCard);
                if (!creditCardTenderTypes.Any())
                {
                    writer.Write("No CreditCard TenderTypes defined for the channel");
                    status = false;
                }

                var creditCardTenderTypeId = creditCardTenderTypes.Single().TenderTypeId;

                var cartTenderline = new CartTenderLine
                {
                    Currency = channelConfiguration.Currency,
                    TenderTypeId = creditCardTenderTypeId,
                    PaymentCard = paymentCard
                };
                cartTenderline.CardTypeId = cartTenderline.PaymentCard.CardTypes;
                cartTenderline.Amount = cart.TotalAmount;                

                var random = new Random();
                cart.OrderNumber = random.Next(1000, 9999).ToString(CultureInfo.InvariantCulture);
                runtimeManager.OrderManager.CreateOrUpdateCart(cart, CalculationModes.None);

                // create order
                var order = runtimeManager.OrderManager.CreateOrderFromCart(cart.Id, runtimeManager.Customer.AccountNumber, new List<CartTenderLine> { cartTenderline }, runtimeManager.Customer.Email);
                if (order == null || order.Id == string.Empty)
                {
                    writer.Write("Failed to create Sales Order");
                    status = false;
                }
                else 
                {
                    writer.Write(string.Format(CultureInfo.InvariantCulture, "Sales Order with ChannelReferenceId {0} has been successfully created.", order.ChannelReferenceId));
                }
            }

            return status;
        }      
    }   
}
