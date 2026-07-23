import about from './about';
import afterSale from './after-sale';
import customer from './customer';
import dashboard from './dashboard';
import functionPage from './function';
import goods from './goods';
import home from './home';
import login from './login';
import manage from './manage';
import order from './order';
import pickupTask from './pickup-task';
import purchase from './purchase';
import storage from './storage';

const page: App.I18n.Schema['translation']['page'] = {
  about,
  afterSale,
  customer,
  dashboard,
  function: functionPage,
  goods,
  home,
  login,
  manage,
  order,
  pickupTask,
  purchase,
  storage
};

export default page;
