using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NDream.AirConsole.Editor {
    public class SafeAreaTester : EditorWindow {
        private FloatField leftField;
        private FloatField topField;
        private FloatField widthField;
        private FloatField heightField;
        private Button applyButton;
        private Button resetButton;
        private Button aspect16By9Button;

        private bool wasConnected;

        [MenuItem("Window/AirConsole/Tester SafeArea")]
        public static void ShowWindow() {
            SafeAreaTester wnd = GetWindow<SafeAreaTester>();
            wnd.titleContent = new GUIContent("SafeArea Tester");
        }

        public void CreateGUI() {
            VisualElement root = rootVisualElement;

            leftField = new FloatField("Left") { value = 0  };
            topField = new FloatField("Top") { value = 0 };
            widthField = new FloatField("Width") { value = 1 };
            heightField = new FloatField("Height") { value = 1 };
            applyButton = new Button(OnApplyButtonClicked) {
                text = "Apply",
                focusable = false
            };
            resetButton = new Button(OnResetButtonClicked) {
                text = "Reset",
                focusable = false
            };
            aspect16By9Button = new Button(OnSet16By9Clicked) {
                text = "16x9",
                focusable = false
            };

            leftField.value = 0;
            topField.value = 0;
            widthField.value = 1;
            heightField.value = 1;

            Label titleLabel = new("Safe Area Tester");
            root.Add(titleLabel);
            root.Add(leftField);
            root.Add(topField);
            root.Add(widthField);
            root.Add(heightField);

            VisualElement flexBoxHorizontal = CreateFlexBoxHorizontal();
            flexBoxHorizontal.Add(applyButton);
            flexBoxHorizontal.Add(resetButton);
            flexBoxHorizontal.Add(aspect16By9Button);
            root.Add(flexBoxHorizontal);

            Ensure01FloatRange(topField);
            Ensure01FloatRange(leftField);
            Ensure01FloatRange(widthField);
            Ensure01FloatRange(heightField);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private VisualElement CreateFlexBoxHorizontal() {
            VisualElement flexBoxHorizontal = new(); 
            flexBoxHorizontal.style.flexDirection = FlexDirection.Row;
            return flexBoxHorizontal;

        }

        private static void Ensure01FloatRange(FloatField field) =>
            field.RegisterValueChangedCallback(evt => {
                field.value = Mathf.Clamp01(evt.newValue);
                evt.PreventDefault();
            });
        
        private void OnKeyDown(KeyDownEvent evt) {
            if (wasConnected) {
                if (AirConsole.instance) {
                    wasConnected = true;
                    AirConsole.instance.OnSafeAreaChanged += OnSafeAreaChanged;
                }
            }
            switch (evt.keyCode) {
                case KeyCode.Tab: {
                    Focusable currentFocusedElement = rootVisualElement.focusController.focusedElement;
                    if (currentFocusedElement == leftField) {
                        topField.Focus();
                    } else if (currentFocusedElement == topField) {
                        widthField.Focus();
                    } else if (currentFocusedElement == widthField) {
                        heightField.Focus();
                    } else if (currentFocusedElement == heightField) {
                        leftField.Focus();
                    }

                    break;
                }
                case KeyCode.R: {
                    OnResetButtonClicked();
                    evt.PreventDefault();
                    break;
                }
                case KeyCode.A: {
                    OnSet16By9Clicked();
                    evt.PreventDefault();
                    break;
                }
                case KeyCode.Return:
                case KeyCode.KeypadEnter: {
                    OnApplyButtonClicked();
                    evt.PreventDefault();
                    break;
                }
            }
        }

        private void OnSafeAreaChanged(Rect obj) {
            leftField.value = obj.x;
            topField.value = obj.y;
            widthField.value = obj.width;
            heightField.value = obj.height;
        }

        private void OnEnable() {
            if (AirConsole.instance) {
                wasConnected = true;
                AirConsole.instance.OnSafeAreaChanged += OnSafeAreaChanged;
                
            } 
        }

        private void OnDisable() {
            if(AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= OnSafeAreaChanged;
            }
        }


        private void OnApplyButtonClicked() {
            JObject msg = new();
            JObject safeAreaObj = new() {
                ["left"] = Mathf.Clamp01(leftField.value),
                ["top"] = Mathf.Clamp01(topField.value),
                ["width"] = Mathf.Clamp01(widthField.value),
                ["height"] = Mathf.Clamp01(heightField.value)
            };
            msg["safeArea"] = safeAreaObj;
            AirConsole.instance.SetSafeArea(msg);

            Debug.Log($"Apply button clicked for safe area: {safeAreaObj}");
        }

        private void OnResetButtonClicked() {
            leftField.value = 0;
            topField.value = 0;
            widthField.value = 1;
            heightField.value = 1;
            OnApplyButtonClicked();
        }

        private void OnSet16By9Clicked() {
            const float aspect16By9 = 16.0f / 9.0f;
            const float aspect9By16 = 9.0f / 16.0f;
            leftField.value = 0;
            topField.value = 0;

            Vector2 screenSize = Handles.GetMainGameViewSize();
            float width = screenSize.x;
            float height = screenSize.y;
            float aspect = width/height;
            if (aspect > aspect16By9) {
                heightField.value = 1;
                widthField.value = aspect16By9 * height / width;
            } else {
                widthField.value = 1;
                heightField.value = aspect9By16 * width / height;
            }
            OnApplyButtonClicked();
        }
    }
}