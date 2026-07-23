declare namespace App {
  namespace I18n {
    interface PageAbout {
      devDep: string;
      introduction: string;
      prdDep: string;
      projectInfo: {
        githubLink: string;
        latestBuildTime: string;
        previewLink: string;
        title: string;
        version: string;
      };
      title: string;
    }
  }
}
