using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 定义商品分类树和税务分类的维护用例。
/// </summary>
public interface IGoodsTypeService : ITreeBaseDataService<GoodsTypeDto, CreateGoodsTypeDto, UpdateGoodsTypeDto, GoodsTypeQueryParameters>;
