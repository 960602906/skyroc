import type { RouteKey } from '@soybean-react/vite-plugin-react-router';
import ElegantReactRouter from '@soybean-react/vite-plugin-react-router';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
interface RouteMeta extends Record<string | number, unknown> {}

interface RouteMetaPreset {
  activeMenu?: string;
  hideInMenu?: boolean;
  icon?: string;
  order?: number;
}

/** 路由菜单图标与排序，对齐 docs/前端菜单分布.md */
const ROUTE_META_PRESETS: Record<string, RouteMetaPreset> = {
  '(base)_about': { hideInMenu: true },
  // —— 首页看板 ——
  '(base)_dashboard': { icon: 'mdi:view-dashboard-outline', order: 1 },
  '(base)_dashboard_ops': { icon: 'mdi:clipboard-check-outline', order: 2 },

  '(base)_dashboard_overview': { icon: 'mdi:chart-line', order: 1 },
  // —— 配送中心 ——
  '(base)_delivery': { icon: 'mdi:truck-fast-outline', order: 5 },
  '(base)_delivery_master': { icon: 'mdi:card-account-details-outline', order: 2 },
  '(base)_delivery_master_carriers': { icon: 'mdi:domain', order: 1 },
  '(base)_delivery_master_drivers': { icon: 'mdi:account-tie-hat', order: 2 },
  '(base)_delivery_master_routes': { icon: 'mdi:map-marker-path', order: 3 },
  '(base)_delivery_ops': { icon: 'mdi:clipboard-text-outline', order: 1 },
  '(base)_delivery_ops_exceptions': { icon: 'mdi:alert-octagon-outline', order: 2 },

  '(base)_delivery_ops_tasks': { icon: 'mdi:truck-check-outline', order: 1 },
  // —— 财务中心 ——
  '(base)_finance': { icon: 'mdi:cash-multiple', order: 6 },
  '(base)_finance_customer': { icon: 'mdi:account-cash-outline', order: 1 },

  '(base)_finance_customer_bills': { icon: 'mdi:file-table-outline', order: 1 },
  '(base)_finance_customer_settlements': { icon: 'mdi:cash-check', order: 2 },
  '(base)_finance_supplier': { icon: 'mdi:storefront-outline', order: 2 },
  '(base)_finance_supplier_bills': { icon: 'mdi:file-table-outline', order: 1 },
  '(base)_finance_supplier_settlements': { icon: 'mdi:cash-check', order: 2 },
  '(base)_function': { hideInMenu: true },
  // —— 基础资料 ——
  '(base)_master': { icon: 'mdi:database-outline', order: 9 },
  '(base)_master_customer': { icon: 'mdi:account-group-outline', order: 2 },
  '(base)_master_customer_companies': { icon: 'mdi:office-building-outline', order: 1 },
  '(base)_master_customer_companies_detail': { activeMenu: '/master/customer/companies', hideInMenu: true },
  '(base)_master_customer_companies_detail_[id]': { activeMenu: '/master/customer/companies', hideInMenu: true },
  '(base)_master_customer_list': { icon: 'mdi:format-list-bulleted', order: 2 },
  '(base)_master_customer_operate': { activeMenu: '/master/customer/list', hideInMenu: true },
  '(base)_master_customer_operate_[id]': { activeMenu: '/master/customer/list', hideInMenu: true },
  '(base)_master_customer_sub-accounts': { icon: 'mdi:account-multiple-outline', order: 4 },
  '(base)_master_customer_sub-accounts_detail': { activeMenu: '/master/customer/sub-accounts', hideInMenu: true },
  '(base)_master_customer_sub-accounts_detail_[id]': {
    activeMenu: '/master/customer/sub-accounts',
    hideInMenu: true
  },
  '(base)_master_customer_tags': { icon: 'mdi:tag-multiple-outline', order: 3 },
  '(base)_master_customer_tags_detail': { activeMenu: '/master/customer/tags', hideInMenu: true },
  '(base)_master_customer_tags_detail_[id]': { activeMenu: '/master/customer/tags', hideInMenu: true },

  '(base)_master_goods': { icon: 'mdi:shopping-outline', order: 1 },
  '(base)_master_goods_detail': { activeMenu: '/master/goods/list', hideInMenu: true },
  '(base)_master_goods_detail_[id]': { activeMenu: '/master/goods/list', hideInMenu: true },
  '(base)_master_goods_list': { icon: 'mdi:format-list-bulleted', order: 2 },
  '(base)_master_goods_operate': { activeMenu: '/master/goods/list', hideInMenu: true },
  '(base)_master_goods_operate_[id]': { activeMenu: '/master/goods/list', hideInMenu: true },
  '(base)_master_goods_types': { icon: 'mdi:shape-outline', order: 1 },
  '(base)_master_goods_types_detail': { activeMenu: '/master/goods/types', hideInMenu: true },
  '(base)_master_goods_types_detail_[id]': { activeMenu: '/master/goods/types', hideInMenu: true },
  '(base)_master_goods_units': { icon: 'mdi:ruler', order: 3 },
  '(base)_master_goods_units_detail': { activeMenu: '/master/goods/units', hideInMenu: true },
  '(base)_master_goods_units_detail_[id]': { activeMenu: '/master/goods/units', hideInMenu: true },
  '(base)_master_pricing': { icon: 'mdi:tag-outline', order: 3 },
  '(base)_master_pricing_protocols': { icon: 'mdi:file-sign', order: 2 },
  '(base)_master_pricing_protocols_detail': { activeMenu: '/master/pricing/protocols', hideInMenu: true },
  '(base)_master_pricing_protocols_detail_[id]': { activeMenu: '/master/pricing/protocols', hideInMenu: true },
  '(base)_master_pricing_quotations': { icon: 'mdi:file-document-outline', order: 1 },
  '(base)_master_pricing_quotations_detail': { activeMenu: '/master/pricing/quotations', hideInMenu: true },
  '(base)_master_pricing_quotations_detail_[id]': { activeMenu: '/master/pricing/quotations', hideInMenu: true },
  '(base)_master_supply': { icon: 'mdi:truck-outline', order: 4 },
  '(base)_master_supply_purchase-rules': { icon: 'mdi:clipboard-list-outline', order: 3 },
  '(base)_master_supply_purchase-rules_detail': { activeMenu: '/master/supply/purchase-rules', hideInMenu: true },
  '(base)_master_supply_purchase-rules_detail_[id]': {
    activeMenu: '/master/supply/purchase-rules',
    hideInMenu: true
  },
  '(base)_master_supply_purchasers': { icon: 'mdi:account-tie-outline', order: 2 },
  '(base)_master_supply_purchasers_detail': { activeMenu: '/master/supply/purchasers', hideInMenu: true },
  '(base)_master_supply_purchasers_detail_[id]': { activeMenu: '/master/supply/purchasers', hideInMenu: true },
  '(base)_master_supply_suppliers': { icon: 'mdi:truck-delivery-outline', order: 1 },
  '(base)_master_supply_suppliers_detail': { activeMenu: '/master/supply/suppliers', hideInMenu: true },
  '(base)_master_supply_suppliers_detail_[id]': { activeMenu: '/master/supply/suppliers', hideInMenu: true },

  '(base)_master_supply_wares': { icon: 'mdi:archive-outline', order: 4 },
  '(base)_master_supply_wares_detail': { activeMenu: '/master/supply/wares', hideInMenu: true },
  '(base)_master_supply_wares_detail_[id]': { activeMenu: '/master/supply/wares', hideInMenu: true },
  '(base)_multi-menu': { hideInMenu: true },
  // —— 订单中心 ——
  '(base)_orders': { icon: 'mdi:clipboard-list-outline', order: 2 },
  '(base)_orders_after-sales': { icon: 'mdi:backup-restore', order: 2 },
  '(base)_orders_after-sales_detail': { activeMenu: '/orders/after-sales', hideInMenu: true },
  '(base)_orders_after-sales_detail_[id]': { activeMenu: '/orders/after-sales', hideInMenu: true },
  '(base)_orders_after-sales_operate': { activeMenu: '/orders/after-sales', hideInMenu: true },
  '(base)_orders_after-sales_operate_[id]': { activeMenu: '/orders/after-sales', hideInMenu: true },

  '(base)_orders_detail': { hideInMenu: true },
  '(base)_orders_detail_[id]': { activeMenu: '/orders/list', hideInMenu: true },
  '(base)_orders_edit': { hideInMenu: true },
  '(base)_orders_edit_[id]': { activeMenu: '/orders/list', hideInMenu: true },
  '(base)_orders_list': { icon: 'mdi:format-list-bulleted', order: 1 },
  '(base)_orders_pickup-tasks': { icon: 'mdi:package-variant-closed', order: 3 },
  '(base)_orders_pickup-tasks_detail': { activeMenu: '/orders/pickup-tasks', hideInMenu: true },
  '(base)_orders_pickup-tasks_detail_[id]': { activeMenu: '/orders/pickup-tasks', hideInMenu: true },
  '(base)_orders_pickup-tasks_operate': { activeMenu: '/orders/pickup-tasks', hideInMenu: true },
  '(base)_orders_pickup-tasks_operate_[id]': { activeMenu: '/orders/pickup-tasks', hideInMenu: true },
  '(base)_projects': { hideInMenu: true },
  // —— 采购中心 ——
  '(base)_purchase': { icon: 'mdi:cart-arrow-down', order: 3 },
  '(base)_purchase_orders': { icon: 'mdi:file-document-outline', order: 2 },
  '(base)_purchase_plans': { icon: 'mdi:calendar-check-outline', order: 1 },
  '(base)_purchase_plans_detail': { activeMenu: '/purchase/plans', hideInMenu: true },
  '(base)_purchase_plans_detail_[id]': { activeMenu: '/purchase/plans', hideInMenu: true },
  // —— 报表分析 ——
  '(base)_reports': { icon: 'mdi:chart-box-outline', order: 8 },
  '(base)_reports_after-sales': { icon: 'mdi:backup-restore', order: 2 },
  '(base)_reports_purchase': { icon: 'mdi:cart-arrow-down', order: 4 },
  '(base)_reports_purchase_goods': { icon: 'mdi:package-variant', order: 1 },

  '(base)_reports_purchase_purchasers': { icon: 'mdi:account-tie-outline', order: 3 },
  '(base)_reports_purchase_suppliers': { icon: 'mdi:truck-outline', order: 2 },
  '(base)_reports_sales': { icon: 'mdi:chart-line', order: 1 },
  '(base)_reports_sales_areas': { icon: 'mdi:map-outline', order: 4 },
  '(base)_reports_sales_categories': { icon: 'mdi:shape-outline', order: 2 },
  '(base)_reports_sales_customers': { icon: 'mdi:account-group-outline', order: 3 },
  '(base)_reports_sales_goods': { icon: 'mdi:package-variant', order: 1 },
  '(base)_reports_stock': { icon: 'mdi:warehouse', order: 3 },
  '(base)_reports_stock_daily': { icon: 'mdi:calendar-today', order: 1 },
  '(base)_reports_stock_daily-goods': { icon: 'mdi:calendar-month-outline', order: 2 },
  // —— 库存中心 ——
  '(base)_storage': { icon: 'mdi:warehouse', order: 4 },
  '(base)_storage_in': { icon: 'mdi:inbox-arrow-down', order: 1 },
  '(base)_storage_in_other': { icon: 'mdi:inbox-full-outline', order: 2 },
  '(base)_storage_in_purchase': { icon: 'mdi:truck-check-outline', order: 1 },
  '(base)_storage_in_sales-return': { icon: 'mdi:keyboard-return', order: 3 },
  '(base)_storage_out': { icon: 'mdi:inbox-arrow-up', order: 2 },
  '(base)_storage_out_other': { icon: 'mdi:package-up', order: 3 },
  '(base)_storage_out_purchase-return': { icon: 'mdi:truck-remove-outline', order: 2 },
  '(base)_storage_out_sale': { icon: 'mdi:truck-delivery-outline', order: 1 },
  '(base)_storage_query': { icon: 'mdi:database-search-outline', order: 3 },
  '(base)_storage_query_batches': { icon: 'mdi:layers-outline', order: 3 },
  '(base)_storage_query_ledgers': { icon: 'mdi:book-open-outline', order: 4 },
  '(base)_storage_query_overview': { icon: 'mdi:view-dashboard-variant-outline', order: 2 },
  '(base)_storage_query_stocktaking': { icon: 'mdi:clipboard-check-outline', order: 1 },

  // —— 系统管理 ——
  '(base)_system': { icon: 'mdi:cog-outline', order: 10 },
  '(base)_system_auth': { icon: 'mdi:shield-account-outline', order: 1 },
  '(base)_system_auth_departments': { icon: 'mdi:sitemap-outline', order: 4 },
  '(base)_system_auth_menus': { icon: 'mdi:menu', order: 3 },
  '(base)_system_auth_roles': { icon: 'mdi:account-key-outline', order: 2 },
  '(base)_system_auth_roles_[...slug]': { activeMenu: '/system/auth/roles', hideInMenu: true },
  '(base)_system_auth_users': { icon: 'mdi:account-outline', order: 1 },
  '(base)_system_auth_users_[id]': { activeMenu: '/system/auth/users', hideInMenu: true },
  '(base)_system_ops': { icon: 'mdi:toolbox-outline', order: 2 },
  '(base)_system_ops_logs': { icon: 'mdi:text-box-outline', order: 3 },
  '(base)_system_ops_logs_logins': { icon: 'mdi:login', order: 2 },
  '(base)_system_ops_logs_operations': { icon: 'mdi:history', order: 1 },
  '(base)_system_ops_notices': { icon: 'mdi:bell-outline', order: 2 },
  '(base)_system_ops_settings': { icon: 'mdi:tune-variant', order: 1 },
  '(base)_system_tools': { icon: 'mdi:printer-outline', order: 3 },
  '(base)_system_tools_files': { icon: 'mdi:folder-outline', order: 3 },
  '(base)_system_tools_import-export': { icon: 'mdi:swap-horizontal', order: 2 },
  '(base)_system_tools_print-templates': { icon: 'mdi:printer', order: 1 },

  // —— 溯源中心 ——
  '(base)_traceability': { icon: 'mdi:qrcode-scan', order: 7 },
  '(base)_traceability_inspection-reports': { icon: 'mdi:file-certificate-outline', order: 1 },
  '(base)_traceability_push-logs': { icon: 'mdi:cloud-upload-outline', order: 3 },
  '(base)_traceability_records': { icon: 'mdi:timeline-text-outline', order: 2 },
  // —— 个人中心 / 演示（不进侧栏） ——
  '(base)_user-center': { hideInMenu: true }
};

export function setupElegantRouter() {
  return ElegantReactRouter({
    customRoutes: {
      names: [
        'exception_403',
        'exception_404',
        'exception_500',
        'document_project',
        'document_project-link',
        'document_react',
        'document_vite',
        'document_unocss',
        'document_proComponents',
        'document_antd',
        'document_ui'
      ]
    },
    onRouteMetaGen(routeName) {
      const key = routeName as RouteKey;

      const constantRoutes: Array<RouteKey | string> = [
        'root',
        '403',
        '404',
        '500',
        'iframe-page',
        'notFound',
        '(blank)_login',
        '(blank)_login_code-login',
        '(blank)_login_register',
        '(blank)_login_reset-pwd',
        '(blank)_login-out'
      ];

      const meta: Partial<RouteMeta> = {
        i18nKey: `route.${key}` as App.I18n.I18nKey,
        title: key
      };

      if (constantRoutes.includes(key)) {
        meta.constant = true;
      }

      const preset = ROUTE_META_PRESETS[key];
      if (preset) {
        Object.assign(meta, preset);
      }

      return meta;
    }
  });
}
