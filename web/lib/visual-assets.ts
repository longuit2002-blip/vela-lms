export const visualAssets = {
  brand: {
    mark: "/vela-assets/brand/vela-mark.svg",
    wordmark: "/vela-assets/brand/vela-wordmark.svg",
  },
  icons: {
    home: "/vela-assets/icons/home.svg",
    report: "/vela-assets/icons/report.svg",
    users: "/vela-assets/icons/users.svg",
    publish: "/vela-assets/icons/publish.svg",
    poc: "/vela-assets/icons/poc.svg",
    bell: "/vela-assets/icons/bell.svg",
    help: "/vela-assets/icons/help.svg",
    assigned: "/vela-assets/icons/assigned.svg",
    learning: "/vela-assets/icons/learning.svg",
    rank: "/vela-assets/icons/rank.svg",
    complete: "/vela-assets/icons/complete.svg",
    organization: "/vela-assets/icons/organization.svg",
    scope: "/vela-assets/icons/scope.svg",
    alert: "/vela-assets/icons/alert.svg",
    data: "/vela-assets/icons/data.svg",
    branch: "/vela-assets/icons/branch.svg",
    aiSource: "/vela-assets/icons/ai-source.svg",
    document: "/vela-assets/icons/document.svg",
  },
} as const;

export type VisualIconName = keyof typeof visualAssets.icons;
