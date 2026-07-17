export interface ModulesProps {
  onClose: () => void;
  open: boolean;
  roleId: string;
}

export interface SimpleRoute {
  buttons?: Api.SystemManage.MenuButton[];
  children?: SimpleRoute[];
  icon: string;
  key: string;
  title: string;
}
